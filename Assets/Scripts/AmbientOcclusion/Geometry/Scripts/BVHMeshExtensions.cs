using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Random = System.Random;

namespace AmbientOcclusion.Geometry.Scripts
{
    public static class BVHMeshExtensions
    {
        private const float Delta = 1e-6f;

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

            bool splitLeafNodes = false;

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
                    splitLeafNodes = true;
                    break;
                }
            }

            while (splitLeafNodes && searchQue.Count < numberOfNodes)
            {
                bool didNotSplit = true;
                int numberOfNodesInQueue = searchQue.Count;

                for (int i = 0; i < numberOfNodesInQueue; i++)
                {
                    (BVHNodeMesh node, bool inSetA) = searchQue.Dequeue();

                    if (node.Split(out BVHNodeMesh leftSplitNode, out BVHNodeMesh rightSplitNode))
                    {
                        searchQue.Enqueue((leftSplitNode, inSetA));
                        searchQue.Enqueue((rightSplitNode, inSetA));

                        didNotSplit = false;
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

                if (didNotSplit)
                {
                    break;
                }
            }

            List<int> groupA = new List<int>();
            List<int> groupB = new List<int>();

            List<BVHNodeMesh> nodesList = new List<BVHNodeMesh>();

            int countIndex = 0;

            while (searchQue.Count > 0)
            {
                (BVHNodeMesh node, bool insetA) = searchQue.Dequeue();

                nodesList.Add(node);
                if (insetA)
                {
                    groupA.Add(countIndex);
                }
                else
                {
                    groupB.Add(countIndex);
                }

                countIndex++;
            }

            return (nodesList, new Bipartition(groupA, groupB));
        }

        private static (Bounds bounds, int primitiveCount) GetPartitionInfo(List<BVHNodeMesh> nodes,
            IEnumerable<int> group)
        {
            using IEnumerator<int> e = group.GetEnumerator();

            if (!e.MoveNext())
            {
                throw new InvalidOperationException("Group must contain at least one element.");
            }

            int firstIndex = e.Current;
            Bounds bounds = nodes[firstIndex].bounds;
            int primitiveCount = nodes[firstIndex].nPrimitives;

            while (e.MoveNext())
            {
                int idx = e.Current;
                bounds.Encapsulate(nodes[idx].bounds);
                primitiveCount += nodes[idx].nPrimitives;
            }

            return (bounds, primitiveCount);
        }

        private static float GetSAHScoreFromGroup(List<BVHNodeMesh> searchingGroup, Bipartition partition)
        {
            (Bounds boundsA, int countA) = GetPartitionInfo(searchingGroup, partition.groupA);
            (Bounds boundsB, int countB) = GetPartitionInfo(searchingGroup, partition.groupB);

            float cost = boundsA.SurfaceArea() * countA;
            cost += boundsB.SurfaceArea() * countB;

            return cost;
        }

        private static HashSet<int> GetMoveableNode(List<BVHNodeMesh> searchingGroup, BVHNodeMesh startNode)
        {
            const int nItems = 8;

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
        

        // a small DTO to hold the results
        class PartitionStats
        {
            public Bounds? StaticBounds { get; }
            public int StaticCount { get; }
            public HashSet<int> StaticIndices { get; }
            public HashSet<int> MoveableNodes { get; }

            public PartitionStats(Bounds? staticBounds, int staticCount, HashSet<int> staticIndices,
                HashSet<int> moveableNodes)
            {
                StaticBounds = staticBounds;
                StaticCount = staticCount;
                StaticIndices = staticIndices;
                MoveableNodes = moveableNodes;
            }
        }

        private static PartitionStats ComputePartitionStats(IEnumerable<int> group, ISet<int> moveableNodes,
            List<BVHNodeMesh> searchingGroup)
        {
            List<int> staticNodes = new();
            HashSet<int> moveNodes = new();

            foreach (int nodeIndex in group)
            {
                if (moveableNodes.Contains(nodeIndex))
                {
                    moveNodes.Add(nodeIndex);
                }
                else
                {
                    staticNodes.Add(nodeIndex);
                }
            }

            if (staticNodes.Count == 0)
            {
                return new PartitionStats(null, 0, new HashSet<int>(), moveNodes);
            }

            Bounds combinedBounds = searchingGroup[staticNodes[0]].bounds;
            int primitiveCount = searchingGroup[staticNodes[0]].nPrimitives;
            for (int i = 1; i < staticNodes.Count; i++)
            {
                BVHNodeMesh mesh = searchingGroup[staticNodes[i]];
                combinedBounds.Encapsulate(mesh.bounds);
                primitiveCount += mesh.nPrimitives;
            }

            HashSet<int> staticIndices = new HashSet<int>(staticNodes);

            return new PartitionStats(combinedBounds, primitiveCount, staticIndices, moveNodes);
        }


        private static bool SearchForBestPartition(BVHNodeMesh searchNode, out List<BVHNodeMesh> leftGroup,
            out List<BVHNodeMesh> rightGroup)
        {
            float baseBest = searchNode.left.nPrimitives * searchNode.left.bounds.SurfaceArea() +
                             searchNode.right.nPrimitives * searchNode.right.bounds.SurfaceArea();

            (List<BVHNodeMesh> searchingGroup, Bipartition bestPartitionFound) = GetNNodes(searchNode, 16);
            float bestCostFound = GetSAHScoreFromGroup(searchingGroup, bestPartitionFound);

            HashSet<int> moveableNodes = GetMoveableNode(searchingGroup, searchNode);

            var statsA = ComputePartitionStats(bestPartitionFound.groupA, moveableNodes, searchingGroup);
            Bounds? staticABounds = statsA.StaticBounds;
            int staticACount = statsA.StaticCount;
            HashSet<int> staticInA = statsA.StaticIndices;
            HashSet<int> moveableInA = statsA.MoveableNodes;

            var statsB = ComputePartitionStats(bestPartitionFound.groupB, moveableNodes, searchingGroup);
            Bounds? staticBBounds = statsB.StaticBounds;
            int staticBCount = statsB.StaticCount;
            HashSet<int> staticInB = statsB.StaticIndices;
            HashSet<int> moveableInB = statsB.MoveableNodes;

            List<int> moveableList = moveableNodes.ToList();
            int total = moveableList.Count;

            void EvaluateAndCompare(IEnumerable<int> candidateA, IEnumerable<int> candidateB)
            {
                (Bounds bounds, int primitiveCount)? groupAInfo = null;
                (Bounds bounds, int primitiveCount)? groupBInfo = null;
                
                if (candidateA.Any())
                {
                    groupAInfo = GetPartitionInfo(searchingGroup, candidateA);
                }

                if (candidateB.Any())
                {
                    groupBInfo = GetPartitionInfo(searchingGroup, candidateB);
                }
                
                if (staticABounds.HasValue)
                {
                    Bounds tempBounds = groupAInfo?.bounds ?? staticABounds.Value;
                    tempBounds.Encapsulate(staticABounds.Value);
                    
                    int tempCount = groupAInfo?.primitiveCount + staticACount ?? staticACount;
                    groupAInfo = (tempBounds, tempCount);
                }

                if (staticBBounds.HasValue)
                {
                    Bounds tempBounds = groupBInfo?.bounds ?? staticBBounds.Value;
                    tempBounds.Encapsulate(staticBBounds.Value);
                    
                    int tempCount = groupBInfo?.primitiveCount + staticBCount ?? staticBCount;
                    groupBInfo = (tempBounds, tempCount);
                }

                if (groupAInfo == null || groupBInfo == null)
                {
                    return;
                }

                if (groupAInfo.Value.primitiveCount == 0 || groupBInfo.Value.primitiveCount == 0)
                {
                    return;
                }

                float candidateCost = groupAInfo.Value.bounds.SurfaceArea() * groupAInfo.Value.primitiveCount +
                                      groupBInfo.Value.bounds.SurfaceArea() * groupBInfo.Value.primitiveCount;

                if (candidateCost + Delta >= bestCostFound)
                {
                    return;
                }

                bestCostFound = candidateCost;

                HashSet<int> newFullGroupA = new HashSet<int>(staticInA);
                newFullGroupA.UnionWith(candidateA);

                HashSet<int> newFullGroupB = new HashSet<int>(staticInB);
                newFullGroupB.UnionWith(candidateB);

                bestPartitionFound = new Bipartition(newFullGroupA, newFullGroupB);
            }
            
            for (int i = 0; i < total; i++)
            {
                int node1 = moveableList[i];

                for (int j = i + 1; j < total; j++)
                {
                    int node2 = moveableList[j];
                    
                    HashSet<int> baseMoveableA = new HashSet<int>(moveableInA);
                    baseMoveableA.Remove(node1);
                    baseMoveableA.Remove(node2);

                    HashSet<int> baseMoveableB = new HashSet<int>(moveableInB);
                    baseMoveableB.Remove(node1);
                    baseMoveableB.Remove(node2);
                    
                    HashSet<int> case1A = new HashSet<int>(baseMoveableA) { node1, node2 };
                    EvaluateAndCompare(case1A, baseMoveableB);
                    
                    HashSet<int> case2B = new HashSet<int>(baseMoveableB) { node1, node2 };
                    EvaluateAndCompare(baseMoveableA, case2B);
                    
                    HashSet<int> case3A = new HashSet<int>(baseMoveableA) { node1 };
                    HashSet<int> case3B = new HashSet<int>(baseMoveableB) { node2 };
                    EvaluateAndCompare(case3A, case3B);

                    HashSet<int> case4A = new HashSet<int>(baseMoveableA) { node2 };
                    HashSet<int> case4B = new HashSet<int>(baseMoveableB) { node1 };
                    EvaluateAndCompare(case4A, case4B);
                }
            }
            
            leftGroup = new List<BVHNodeMesh>(bestPartitionFound.groupA.Count);
            rightGroup = new List<BVHNodeMesh>(bestPartitionFound.groupB.Count);

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
            int[] cachedTriangleIndices)
        {
            int[] tempReorderedBuffer = new int[cachedTriangleIndices.Length];
            return OptimizeNodeRoutine(node, cachedTriangleArray, cachedTriangleIndices, 0, tempReorderedBuffer);
        }

        private static BVHNodeMesh OptimizeNodeRoutine(
            BVHNodeMesh searchNode,
            Triangle[] cachedTriangleArray,
            int[] cachedTriangleIndices,
            int depth,
            int[] tempReorderedBuffer
        )
        {
            if (searchNode.leaf != null)
            {
                return searchNode;
            }

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

                int writeCount = 0;

                int leftStart = searchNode.range.start;
                CollectGroupIndices(leftGroup, tempReorderedBuffer, ref writeCount);
                int leftEnd = leftStart + writeCount;

                int rightStart = leftEnd;
                CollectGroupIndices(rightGroup, tempReorderedBuffer, ref writeCount);
                int rightEnd = searchNode.range.end; // unchanged

                Array.Copy(
                    sourceArray: tempReorderedBuffer,
                    sourceIndex: 0,
                    destinationArray: cachedTriangleIndices,
                    destinationIndex: searchNode.range.start,
                    length: writeCount
                );

                var leftRange = new ArrayRange(leftStart, leftEnd);
                var rightRange = new ArrayRange(rightStart, rightEnd);

                DrawBound(searchNode.bounds, Color.blue, Vector3.zero);
                DrawBound(searchNode.left.bounds, Color.red, Vector3.zero);
                DrawBound(searchNode.right.bounds, Color.green, Vector3.zero);

                searchNode.left = new BVHNodeMesh(cachedTriangleArray, cachedTriangleIndices, leftRange);
                searchNode.right = new BVHNodeMesh(cachedTriangleArray, cachedTriangleIndices, rightRange);

                DrawBound(searchNode.bounds, Color.magenta, Vector3.forward);
                DrawBound(searchNode.left.bounds, Color.yellow, Vector3.forward);
                DrawBound(searchNode.right.bounds, Color.cyan, Vector3.forward);

                Debug.Log($"FOUND BETTER PARTITIONS at depth: {depth}");
            }

            searchNode.left = OptimizeNodeRoutine(
                searchNode.left,
                cachedTriangleArray,
                cachedTriangleIndices,
                depth + 1,
                tempReorderedBuffer);

            searchNode.right = OptimizeNodeRoutine(
                searchNode.right,
                cachedTriangleArray,
                cachedTriangleIndices,
                depth + 1,
                tempReorderedBuffer);

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