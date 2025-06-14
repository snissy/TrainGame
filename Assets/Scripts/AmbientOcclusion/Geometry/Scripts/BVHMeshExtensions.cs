using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace AmbientOcclusion.Geometry.Scripts
{
    public static class BVHMeshExtensions
    {
        private static Random randomGenerator = new Random();

        private const float delta = 1e-6f;

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

        private static (List<BVHNodeMesh> nodes, Bipartition) GetNNodes(BVHNodeMesh startNode, int numberOfNodes)
        {
            Queue<(BVHNodeMesh node, bool inSetA)> searchQue = new();

            searchQue.Enqueue((startNode.left, true));
            searchQue.Enqueue((startNode.right, false));

            while (searchQue.Count < numberOfNodes && searchQue.Count > 0)
            {
                int nLeafNodes = 0;
                int numberOfNodesInQueue = searchQue.Count;

                for (int i = 0; i < numberOfNodesInQueue; i++)
                {
                    (BVHNodeMesh node, bool inSetA) = searchQue.Dequeue();

                    if (node.leaf != null)
                    {
                        searchQue.Enqueue((node, inSetA));
                        nLeafNodes++;
                        continue;
                    }

                    if (node.left != null)
                    {
                        searchQue.Enqueue((node.left, inSetA));
                    }

                    if (node.right != null)
                    {
                        searchQue.Enqueue((node.right, inSetA));
                    }

                    if (searchQue.Count == numberOfNodes)
                    {
                        break;
                    }
                }

                if (nLeafNodes == searchQue.Count)
                {
                    for (int i = 0; i < nLeafNodes; i++)
                    {
                        (BVHNodeMesh node, bool inSetA) = searchQue.Dequeue();

                        if (node.Split(out BVHNodeMesh leftSplitNode, out BVHNodeMesh rightSplitNode))
                        {
                            searchQue.Enqueue((leftSplitNode, inSetA));
                            searchQue.Enqueue((rightSplitNode, inSetA));
                        }
                        else
                        {
                            searchQue.Enqueue((node, inSetA));
                        }

                        if (searchQue.Count == numberOfNodes)
                        {
                            break;
                        }
                    }

                    break;
                }
            }

            List<int> groupA = new List<int>();
            List<int> groupB = new List<int>();

            (BVHNodeMesh node, bool inSetA)[] searchQueArray = searchQue.ToArray();

            List<BVHNodeMesh> nodesList = new List<BVHNodeMesh>();

            for (var i = 0; i < searchQueArray.Length; i++)
            {
                (BVHNodeMesh node, bool insetA) = searchQueArray[i];

                nodesList.Add(node);
                if (insetA)
                {
                    groupA.Add(i);
                }
                else
                {
                    groupB.Add(i);
                }
            }

            return (nodesList, new Bipartition(groupA, groupB));
        }

        private static Bounds GetBoundsFromPartition(List<BVHNodeMesh> nodes, List<int> group)
        {
            if (group.Count == 0)
            {
                throw new Exception("This groups should not be empty!!!");
            }

            Bounds bounds = nodes[group[0]].bounds;

            for (var i = 1; i < group.Count; i++)
            {
                bounds.Encapsulate(nodes[group[i]].bounds);
            }

            return bounds;
        }

        private static int GetPrimitiveCountFromPartition(List<BVHNodeMesh> nodes, List<int> group)
        {
            int count = 0;

            for (var i = 0; i < group.Count; i++)
            {
                count += nodes[group[i]].nPrimitives;
            }

            return count;
        }

        private static float GetSAHScoreFromGroup(List<BVHNodeMesh> searchingGroup, Bipartition partition)
        {
            float cost = GetBoundsFromPartition(searchingGroup, partition.groupA).SurfaceArea() *
                         GetPrimitiveCountFromPartition(searchingGroup, partition.groupA);
            cost += GetBoundsFromPartition(searchingGroup, partition.groupB).SurfaceArea() *
                    GetPrimitiveCountFromPartition(searchingGroup, partition.groupB);

            return cost;
        }

        private static HashSet<int> GetMoveableNode(List<BVHNodeMesh> searchingGroup, BVHNodeMesh startNode)
        {
            const int nItems = 64;

            List<(int index, float distance)> res = new List<(int index, float distance)>();

            for (var i = 0; i < searchingGroup.Count; i++)
            {
                BVHNodeMesh node = searchingGroup[i];

                float leftDistance = BoundsExtensions.SgnDistance(node.bounds, startNode.left.bounds);
                float rightDistance = BoundsExtensions.SgnDistance(node.bounds, startNode.right.bounds);

                float edgeDistance = Mathf.Min(leftDistance, rightDistance);

                res.Add((i, edgeDistance));
            }

            return res.OrderByDescending(x => x.distance).Select(x => x.index).Take(nItems).ToHashSet();
        }

        private static bool SearchForBestPartition(BVHNodeMesh searchNode, out List<BVHNodeMesh> leftGroup,
            out List<BVHNodeMesh> rightGroup)
        {
            float baseBest = searchNode.left.nPrimitives * searchNode.left.bounds.SurfaceArea() +
                             searchNode.right.nPrimitives * searchNode.right.bounds.SurfaceArea();

            (List<BVHNodeMesh> searchingGroup, Bipartition bestPartitionFound) = GetNNodes(searchNode, 4096);
            float bestCostFound = GetSAHScoreFromGroup(searchingGroup, bestPartitionFound);

            HashSet<int> moveableNodes = GetMoveableNode(searchingGroup, searchNode);

            /*for (var i = 0; i < searchingGroup.Count; i++)
            {
                BVHNodeMesh nodeMesh = searchingGroup[i];
                Color drawColor = moveableNodes.Contains(i) ? Color.green : Color.red;
                DrawBound(nodeMesh.bounds, drawColor, Vector3.zero);
            }*/

            while (true)
            {
                bool didNotFoundImprovement = true;
                foreach (var candidatePartition in RandomBipartitionGenerator.GetAllAlteredBipartitionsSwap(
                             bestPartitionFound, moveableNodes))
                {
                    float candidateCost = GetSAHScoreFromGroup(searchingGroup, candidatePartition);
                    if (candidateCost + delta > bestCostFound)
                    {
                        continue;
                    }

                    bestPartitionFound = candidatePartition;
                    bestCostFound = candidateCost;
                    didNotFoundImprovement = false;
                }

                if (didNotFoundImprovement)
                {
                    break;
                }
            }

            while (true)
            {
                bool didNotFoundImprovement = true;
                foreach (Bipartition candidatePartition in RandomBipartitionGenerator.GetAllAlteredBipartitionsOneMove(
                             bestPartitionFound))
                {
                    float candidateCost = GetSAHScoreFromGroup(searchingGroup, candidatePartition);
                    if (candidateCost + delta > bestCostFound)
                    {
                        continue;
                    }

                    bestPartitionFound = candidatePartition;
                    bestCostFound = candidateCost;
                    didNotFoundImprovement = false;
                }

                if (didNotFoundImprovement)
                {
                    break;
                }
            }


            leftGroup = new List<BVHNodeMesh>();
            rightGroup = new List<BVHNodeMesh>();

            if (bestCostFound + 1e-4 > baseBest)
            {
                return false;
            }

            foreach (int nodeIndex in bestPartitionFound.groupA)
            {
                leftGroup.Add(searchingGroup[nodeIndex]);
            }

            foreach (int nodeIndex in bestPartitionFound.groupB)
            {
                rightGroup.Add(searchingGroup[nodeIndex]);
            }

            return true;
        }


        public static BVHNodeMesh OptimizeNode(this BVHNodeMesh node, Triangle[] cachedTriangleArray,
            int[] cachedTriangleIndices, ArrayRange arrayRange)
        {
            return OptimizeNodeRoutine(node, cachedTriangleArray, cachedTriangleIndices, arrayRange, 0);
        }

        private static BVHNodeMesh OptimizeNodeRoutine(
            BVHNodeMesh searchNode,
            Triangle[] cachedTriangleArray,
            int[] cachedTriangleIndices,
            ArrayRange arrayRange,
            int depth)
        {
            if (searchNode.leaf != null)
            {
                return searchNode;
            }

            // Attempt to find a better partition for this node
            if (SearchForBestPartition(searchNode, out List<BVHNodeMesh> leftGroup, out List<BVHNodeMesh> rightGroup))
            {
                void CollectGroupIndices(List<BVHNodeMesh> group, int[] tempIndices, ref int count)
                {
                    foreach (BVHNodeMesh node in group)
                    {
                        foreach (int idx in node.range)
                        {
                            tempIndices[count++] = cachedTriangleIndices[idx];
                        }
                    }
                }

                int[] tempReordered = new int[arrayRange.count];
                int writeCount = 0;

                int leftStart = arrayRange.start;
                CollectGroupIndices(leftGroup, tempReordered, ref writeCount);
                int leftEnd = leftStart + writeCount;

                // Collect indices for the right partition
                int rightStart = leftEnd;
                CollectGroupIndices(rightGroup, tempReordered, ref writeCount);
                int rightEnd = arrayRange.end; // unchanged

                Array.Copy(
                    sourceArray: tempReordered,
                    sourceIndex: 0,
                    destinationArray: cachedTriangleIndices,
                    destinationIndex: arrayRange.start,
                    length: writeCount
                );

                // Create new ArrayRange instances for left and right children
                var leftRange = new ArrayRange(leftStart, leftEnd);
                var rightRange = new ArrayRange(rightStart, rightEnd);

                DrawBound(searchNode.bounds, Color.blue, Vector3.zero);
                DrawBound(searchNode.left.bounds, Color.red, Vector3.zero);
                DrawBound(searchNode.right.bounds, Color.green, Vector3.zero);

                // Assign new child BVH nodes with updated ranges
                searchNode.left = new BVHNodeMesh(cachedTriangleArray, cachedTriangleIndices, leftRange);
                searchNode.right = new BVHNodeMesh(cachedTriangleArray, cachedTriangleIndices, rightRange);

                DrawBound(searchNode.bounds, Color.magenta, Vector3.forward);
                DrawBound(searchNode.left.bounds, Color.yellow, Vector3.forward);
                DrawBound(searchNode.right.bounds, Color.cyan, Vector3.forward);

                Debug.Log($"FOUND BETTER PARTITIONS at depth: {depth}");
            }

            // Recurse on left and right children (whether re-partitioned or not)
            searchNode.left = OptimizeNodeRoutine(searchNode.left, cachedTriangleArray, cachedTriangleIndices,
                searchNode.left.range, depth + 1);
            searchNode.right = OptimizeNodeRoutine(searchNode.right, cachedTriangleArray, cachedTriangleIndices,
                searchNode.right.range, depth + 1);

            return searchNode;
        }


        private static void DrawTriangles(BVHNodeMesh node, Vector3 offset)
        {
            Stack<BVHNodeMesh> stack = new Stack<BVHNodeMesh>();
            stack.Push(node.left);
            while (stack.Count > 0)
            {
                var nodeToSearch = stack.Pop();

                if (nodeToSearch.leaf != null)
                {
                    Triangle[] triangles = nodeToSearch.leaf.GetTriangles();

                    DrawTriangles(triangles, Color.red, offset);
                }
                else
                {
                    if (nodeToSearch.left != null)
                    {
                        stack.Push(nodeToSearch.left);
                    }

                    if (nodeToSearch.right != null)
                    {
                        stack.Push(nodeToSearch.right);
                    }
                }
            }

            stack = new Stack<BVHNodeMesh>();
            stack.Push(node.right);
            while (stack.Count > 0)
            {
                var nodeToSearch = stack.Pop();

                if (nodeToSearch.leaf != null)
                {
                    Triangle[] triangles = nodeToSearch.leaf.GetTriangles();

                    DrawTriangles(triangles, Color.green, offset);
                }
                else
                {
                    if (nodeToSearch.left != null)
                    {
                        stack.Push(nodeToSearch.left);
                    }

                    if (nodeToSearch.right != null)
                    {
                        stack.Push(nodeToSearch.right);
                    }
                }
            }
        }

        private static void DrawTriangles(Triangle[] triangles, Color c, Vector3 offset)
        {
            foreach (Triangle triangle in triangles)
            {
                DrawLine(triangle.v0, triangle.v1, c, offset);
                DrawLine(triangle.v1, triangle.v2, c, offset);
                DrawLine(triangle.v2, triangle.v0, c, offset);
            }
        }

        private static void DrawBound(Bounds bounds, Color c, Vector3 offset)
        {
            Vector3[] boundsVertices = BVHUtils.CalculateWorldBoxCorners(bounds);
            for (int i = 0; i < 4; i++)
            {
                Vector3 c1 = boundsVertices[i];
                Vector3 c2 = boundsVertices[(i + 1) % 4];
                DrawLine(c1, c2, c, offset);

                Vector3 c3 = boundsVertices[4 + i];
                Vector3 c4 = boundsVertices[4 + (i + 1) % 4];

                DrawLine(c3, c4, c, offset);
                DrawLine(c1, c3, c, offset);
            }
        }

        private static void DrawLine(Vector3 p0, Vector3 p1, Color c, Vector3 offset)
        {
            Debug.DrawLine(offset + p0, offset + p1, c, 5 * 60);
        }
    }
}