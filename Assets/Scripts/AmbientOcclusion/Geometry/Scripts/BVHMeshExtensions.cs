using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using Random = System.Random;


namespace AmbientOcclusion.Geometry.Scripts

{
    public static class BVHMeshExtensions
    {
        private const float Delta = 1e-6f;

        private static readonly Random RANDOM_SOURCE = new();


        private const float DEFAULT_COST_TRAVERSAL = 1.0f;

        private const float DEFAULT_COST_INTERSECTION = 1.0f; // Assuming intersection is as costly as traversal


        public static float CalculateTotalSAHCost(this BVHNodeMesh node, float costTraversal = DEFAULT_COST_TRAVERSAL,
            float costIntersection = DEFAULT_COST_INTERSECTION)
        {
            return CalculateRecursiveSAHCost(node, costTraversal, costIntersection);
        }

        private static float CalculateRecursiveSAHCost(BVHNodeMesh node, float costTraversal, float costIntersection)

        {
            if (node.leaf != null)

            {
                return node.nPrimitives * costIntersection;
            }


            float costLeft = CalculateRecursiveSAHCost(node.left, costTraversal, costIntersection);

            float costRight = CalculateRecursiveSAHCost(node.right, costTraversal, costIntersection);


            float parentSurfaceArea = node.bounds.SurfaceArea();

            float sahCost;


            if (parentSurfaceArea > 1e-9)

            {
                float probLeft = node.left.bounds.SurfaceArea() / parentSurfaceArea;

                float probRight = node.right.bounds.SurfaceArea() / parentSurfaceArea;
                sahCost = costTraversal + probLeft * costLeft + probRight * costRight;
            }

            else

            {
                sahCost = costTraversal + costLeft + costRight;
            }


            return sahCost;
        }

        public static BVHNodeMesh OptimizeNode(this BVHNodeMesh rootNode)
        {
            const int NumberOfSearchIterations = 128;

            for (int i = 0; i < NumberOfSearchIterations; i++)
            {
                Depth2Tree pickedNode = FindBestNodeToOptimize(rootNode);

                BVHNodeMesh nodeToSplit = pickedNode.nodeToSplit;

                BVHNodeMesh grandchild1 = nodeToSplit.left;
                BVHNodeMesh grandchild2 = nodeToSplit.right;

                pickedNode.parent.TransformInto(pickedNode.nodeToBecomeParent);

                BVHNodeMesh newParentForGrandchild1 = FindBestNodeForReinsertion(rootNode, grandchild1);
                newParentForGrandchild1.MergeWith(grandchild1);

                BVHNodeMesh newParentForGrandchild2 = FindBestNodeForReinsertion(rootNode, grandchild2);
                newParentForGrandchild2.MergeWith(grandchild2);
                rootNode.UpdateBounds();

                Debug.Log($"SAH COST {rootNode.CalculateTotalSAHCost()}");
            }


            // Find the node pair with the shortest distance or sah that is not on the same tree

            (BVHNodeMesh left, BVHNodeMesh right) bestPair = FindNodePairWithSmallestSAH(rootNode);

            void DrawBoundDebug(Bounds bounds, Color c, Vector3 offset)
            {
                Vector3[] boundsVertices = BVHUtils.CalculateWorldBoxCorners(bounds);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 c1 = boundsVertices[i];
                    Vector3 c2 = boundsVertices[(i + 1) % 4];
                    DrawLineDebug(c1, c2, c, offset);

                    Vector3 c3 = boundsVertices[4 + i];
                    Vector3 c4 = boundsVertices[4 + (i + 1) % 4];

                    DrawLineDebug(c3, c4, c, offset);
                    DrawLineDebug(c1, c3, c, offset);
                }
            }

            void DrawLineDebug(Vector3 p0, Vector3 p1, Color c, Vector3 offset)
            {
                Debug.DrawLine(offset + p0, offset + p1, c, 45);
            }

            DrawBoundDebug(bestPair.left.bounds, Color.magenta, Vector3.zero);
            DrawBoundDebug(bestPair.right.bounds, Color.cyan, Vector3.zero);

            return rootNode;
        }

        private static (BVHNodeMesh, BVHNodeMesh) FindNodePairWithSmallestSAH(BVHNodeMesh rootNode)
        {
            (BVHNodeMesh leftNode, BVHNodeMesh rightNode) bestPair = (rootNode.left, rootNode.right);
            float minSAHPair = float.MaxValue;

            Queue<BVHNodeMesh> queue = new();
            queue.Enqueue(rootNode.left);
            Queue<BVHNodeMesh> searchQueue = new Queue<BVHNodeMesh>();

            while (queue.Count > 0)
            {
                BVHNodeMesh toSearch = queue.Dequeue();

                searchQueue.Enqueue(rootNode.right);

                while (searchQueue.Count > 0)
                {
                    BVHNodeMesh toTest = searchQueue.Dequeue();

                    Bounds toTestBounds = toTest.bounds;

                    toTestBounds.Encapsulate(toSearch.bounds);

                    if (toTestBounds.SurfaceArea() < minSAHPair)
                    {
                        minSAHPair = toTestBounds.SurfaceArea();
                        bestPair = (toTest, toSearch);
                    }

                    if (toTest.left != null)
                    {
                        searchQueue.Enqueue(toTest.left);
                    }

                    if (toTest.right != null)
                    {
                        searchQueue.Enqueue(toTest.right);
                    }
                }

                if (toSearch.left != null)
                {
                    queue.Enqueue(toSearch.left);
                }

                if (toSearch.right != null)
                {
                    queue.Enqueue(toSearch.right);
                }
            }

            return bestPair;
        }


        public readonly struct Depth2Tree
        {
            public static Depth2Tree Empty { get; } = new(null, null, null);
            public BVHNodeMesh parent { get; }

            public BVHNodeMesh nodeToSplit { get; }

            public BVHNodeMesh nodeToBecomeParent { get; }

            public Depth2Tree(BVHNodeMesh parent, BVHNodeMesh nodeToSplit, BVHNodeMesh nodeToBecomeParent)
            {
                this.parent = parent;
                this.nodeToSplit = nodeToSplit;
                this.nodeToBecomeParent = nodeToBecomeParent;
            }
        }

        private static Depth2Tree FindBestNodeToOptimize(BVHNodeMesh root)
        {
            Depth2Tree pickedTree = Depth2Tree.Empty;
            float maxScore = float.MinValue;

            Queue<Depth2Tree> queue = new();

            queue.Enqueue(new Depth2Tree(root, root.left, root.right));
            queue.Enqueue(new Depth2Tree(root, root.right, root.left));

            while (queue.Count > 0)
            {
                Depth2Tree current = queue.Dequeue();
                BVHNodeMesh nodeToSplit = current.nodeToSplit;

                float score = MCOMB(nodeToSplit);

                if (score > maxScore)
                {
                    maxScore = score;
                    pickedTree = current;
                }

                if (nodeToSplit.left.leaf != null)
                {
                    continue;
                }

                if (nodeToSplit.right.leaf != null)
                {
                    continue;
                }

                queue.Enqueue(new Depth2Tree(nodeToSplit, nodeToSplit.left, nodeToSplit.right));
                queue.Enqueue(new Depth2Tree(nodeToSplit, nodeToSplit.right, nodeToSplit.left));
            }

            return pickedTree;
        }

        private static BVHNodeMesh FindBestNodeForReinsertion(BVHNodeMesh rootNode, BVHNodeMesh searchNode)
        {
            float bestCost = float.MaxValue;
            BVHNodeMesh bestNode = searchNode;
            float surfaceAreaOfSearchNode = searchNode.bounds.SurfaceArea();
            const float delta = 1e-6f;

            SimplePriorityQue<float, (BVHNodeMesh node, float inducedCost)> searchQue = new();

            searchQue.Insert(1f / delta, (rootNode, 0f));

            while (searchQue.IsNotEmpty)
            {
                (BVHNodeMesh testNode, float inducedCost) = searchQue.DeleteMax();

                if (inducedCost + surfaceAreaOfSearchNode > bestCost)
                {
                    break;
                }

                Bounds mergeBounds = searchNode.bounds;
                mergeBounds.Encapsulate(testNode.bounds);
                float directCost = mergeBounds.SurfaceArea();
                float totalCost = inducedCost + directCost;

                if (totalCost < bestCost)
                {
                    bestCost = totalCost;
                    bestNode = testNode;
                }

                if (testNode.leaf != null)
                {
                    continue;
                }

                float inducedCostForChildren = totalCost - testNode.bounds.SurfaceArea();

                if (inducedCostForChildren + surfaceAreaOfSearchNode < bestCost)
                {
                    float priority = 1f / (delta + inducedCostForChildren);
                    searchQue.Insert(priority, (testNode.left, inducedCostForChildren));
                    searchQue.Insert(priority, (testNode.right, inducedCostForChildren));
                }
            }

            return bestNode;
        }

        private static float SurfaceArea(BVHNodeMesh node) => node.bounds.SurfaceArea();

        public static float MSUM(BVHNodeMesh node)
        {
            float saN = SurfaceArea(node);
            float saL = SurfaceArea(node.left);
            float saR = SurfaceArea(node.right);
            return saN / ((saL + saR) * 0.5f);
        }

        public static float MMIN(BVHNodeMesh node)
        {
            float saN = SurfaceArea(node);
            float saL = SurfaceArea(node.left);
            float saR = SurfaceArea(node.right);
            float minChild = Math.Min(saL, saR);
            return saN / minChild;
        }

        public static float MAREA(BVHNodeMesh node) => SurfaceArea(node);

        public static float MDIST(BVHNodeMesh node)
        {
            return BoundsExtensions.SgnDistance(node.left.bounds, node.right.bounds) / node.bounds.size.magnitude;
        }

        public static float MCOMB(BVHNodeMesh node) => MSUM(node) * MMIN(node) * MAREA(node) * MDIST(node);
    }
}