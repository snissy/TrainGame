using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmbientOcclusion.Geometry.Scripts
{
    public struct BVHArrayNode
    {
        public const int NInts = 4;
        public const int NBounds = 1;

        public int LeftNodeIndex;
        public int RightNodeIndex;
        public Bounds Bounds;
        public int leafIndexStart;
        public int leafIndexEnd;
    }

    public struct BVHMeshGPUInstance
    {
        // This is the geometry information
        public int[] triangleIndices;
        public Triangle[] triangleArray;
        public BVHArrayNode[] Nodes;
    }

    public readonly struct ArrayRange : IEnumerable<int>
    {
        public readonly int start;
        public readonly int end;
        public readonly int count;

        public ArrayRange(int start, int end)
        {
            this.start = start;
            this.end = end;
            this.count = end - start;
        }

        public void Split(out ArrayRange left, out ArrayRange right)
        {
            int midIndex = start + count / 2;
            left = new ArrayRange(start, midIndex);
            right = new ArrayRange(midIndex, end);
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = start; i < end; i++)
            {
                yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return $"S_{start}_E_{end}";
        }
    }

    internal class BVHLeafTriangle
    {
        public readonly Triangle[] globalTriangles;
        public readonly int[] triangleIndices;
        public readonly ArrayRange range;

        public BVHLeafTriangle(Triangle[] globalTriangles, int[] triangleIndices, ArrayRange range)
        {
            this.globalTriangles = globalTriangles;
            this.triangleIndices = triangleIndices;
            this.range = range;
        }

        public bool IntersectRay(Ray testRay, out float resultLambda)
        {
            resultLambda = float.MaxValue;
            bool hitOccurs = false;

            for (int i = range.start; i < range.end; i++)
            {
                bool intersectsRay = globalTriangles[triangleIndices[i]].IntersectRay(testRay, out float hitLambda);
                if (!intersectsRay) continue;
                hitOccurs = true;
                resultLambda = Mathf.Min(resultLambda, hitLambda);
            }

            return hitOccurs;
        }

        public Triangle[] GetTriangles()
        {
            var resultArray = new Triangle[range.count];
            int indexCount = 0;
            for (int i = range.start; i < range.end; i++)
            {
                resultArray[indexCount] = globalTriangles[triangleIndices[i]];
                indexCount++;
            }

            return resultArray;
        }

        private Bounds CalculateBoundsForGroup(HashSet<int> triangleGroupIndices)
        {
            Triangle GetTriangle(int relativeIndex)
            {
                return globalTriangles[triangleIndices[range.start + relativeIndex]];
            }

            var enumerator = triangleGroupIndices.GetEnumerator();

            enumerator.MoveNext();
            Bounds bounds = GetTriangle(enumerator.Current).bounds;

            while (enumerator.MoveNext())
            {
                bounds.Encapsulate(GetTriangle(enumerator.Current).bounds);
            }

            return bounds;
        }

        private Bipartition FindBestPartition()
        {
            float bestCost = float.MaxValue;
            Bipartition bestPartition = Bipartition.emptyPartition;

            foreach (Bipartition partition in BipartitionGenerator.GetBipartitionForN(range.count))
            {
                Bounds groupABounds = CalculateBoundsForGroup(partition.groupA);
                Bounds groupBBounds = CalculateBoundsForGroup(partition.groupB);

                float cost = groupABounds.SurfaceArea() * partition.groupA.Count +
                             groupBBounds.SurfaceArea() * partition.groupB.Count;

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestPartition = partition;
                }
            }

            return bestPartition;
        }
    }

    public class BVHNodeMesh
    {
        private const int LEAF_LIMIT = 4;

        internal BVHLeafTriangle leaf;
        internal Bounds bounds;
        
        internal BVHNodeMesh left;
        internal BVHNodeMesh right;
        
        internal int nPrimitives;

        public BVHNodeMesh(Triangle[] globalTriangles, int[] triangleIndices, ArrayRange range)
        {
            nPrimitives = range.count;
            bounds = globalTriangles[triangleIndices[range.start]].bounds;
            
            for (int i = range.start + 1; i < range.end; i++)
            {
                bounds.Encapsulate(globalTriangles[triangleIndices[i]].bounds);
            }

            if (range.count <= LEAF_LIMIT)
            {
                leaf = new BVHLeafTriangle(globalTriangles, triangleIndices, range);
                return;
            }

            BVHUtils.SplitVolumeSah(bounds,
                globalTriangles,
                triangleIndices,
                range,
                32,
                out ArrayRange leftRange,
                out ArrayRange rightRange);

            left = new BVHNodeMesh(globalTriangles, triangleIndices, leftRange);
            right = new BVHNodeMesh( globalTriangles, triangleIndices, rightRange);
        }
        public void TransformInto(BVHNodeMesh node)
        {
            leaf = node.leaf;
            bounds = node.bounds;
            left = node.left;
            right = node.right;
            nPrimitives = node.nPrimitives;
        }
        
        public void MergeWith(BVHNodeMesh newNode)
        {
            BVHNodeMesh oldSelf = Clone(this);
            
            left = oldSelf;
            right = newNode;
            
            bounds = left.bounds;
            bounds.Encapsulate(right.bounds);
            nPrimitives  = left.nPrimitives + right.nPrimitives;
            leaf = null;
        }
        
        private static BVHNodeMesh Clone(BVHNodeMesh other)
        {
            return new BVHNodeMesh(other);
        }
        
        private BVHNodeMesh(BVHNodeMesh other)
        {
            this.leaf = other.leaf;
            this.bounds = other.bounds;
            this.left = other.left;
            this.right = other.right;
            this.nPrimitives = other.nPrimitives;
        }
        
        public bool IntersectRay(Ray testRay, ref float minLambda, ref int nIntersections)
        {
            if (leaf != null)
            {
                nIntersections += 1;
                if (!leaf.IntersectRay(testRay, out float leafLambda))
                {
                    return false;
                }

                if (minLambda > leafLambda)
                {
                    minLambda = leafLambda;
                }

                return true;
            }

            BVHNodeMesh first = left;
            BVHNodeMesh second = right;

            nIntersections += 1;
            if (!left.bounds.IntersectRay(testRay, out float firstHit))
            {
                firstHit = float.MaxValue;
            }

            nIntersections += 1;
            if (!right.bounds.IntersectRay(testRay, out float secondHit))
            {
                secondHit = float.MaxValue;
            }

            if (secondHit < firstHit)
            {
                (first, second) = (right, left);
                (firstHit, secondHit) = (secondHit, firstHit);
            }

            bool firstIntersects =
                firstHit < minLambda && first.IntersectRay(testRay, ref minLambda, ref nIntersections);
            bool secondIntersects =
                secondHit < minLambda && second.IntersectRay(testRay, ref minLambda, ref nIntersections);

            return firstIntersects || secondIntersects;
        }

        public IEnumerable<Bounds> GetBounds()
        {
            yield return bounds;
            if (leaf != null)
            {
                yield break;
            }

            foreach (Bounds bound in left.GetBounds())
            {
                yield return bound;
            }

            foreach (Bounds bound in right.GetBounds())
            {
                yield return bound;
            }
        }

        public BVHArrayNode[] GetAllNodes()
        {
            List<BVHArrayNode> flatNodes = new List<BVHArrayNode>();

            var traversalQueue = new Queue<BVHNodeMesh>();
            traversalQueue.Enqueue(this);
            int nextNodeIndex = 1;

            while (traversalQueue.Count > 0)
            {
                var currentNode = traversalQueue.Dequeue();

                var arrayNode = new BVHArrayNode
                {
                    Bounds = currentNode.bounds
                };

                if (currentNode.leaf != null)
                {
                    arrayNode.leafIndexStart = currentNode.leaf.range.start;
                    arrayNode.leafIndexEnd = currentNode.leaf.range.end;
                }
                else
                {
                    arrayNode.LeftNodeIndex = nextNodeIndex++;
                    arrayNode.RightNodeIndex = nextNodeIndex++;

                    traversalQueue.Enqueue(currentNode.left);
                    traversalQueue.Enqueue(currentNode.right);
                }

                flatNodes.Add(arrayNode);
            }

            return flatNodes.ToArray();
        }

        public void UpdateBounds()
        {
            if (leaf != null)
            {
                return;
            }
            
            left.UpdateBounds();
            right.UpdateBounds();
            
            this.bounds = left.bounds;
            bounds.Encapsulate(right.bounds);
        }
    }

    public class BVHMesh
    {
        private BVHNodeMesh root;
        private readonly MeshRenderer renderer;
        private readonly Triangle[] cachedTriangleArray;
        private readonly int[] cachedTriangleIndices;

        public MeshRenderer Renderer => renderer;

        public BVHMesh(MeshRenderer meshRenderer)
        {
            renderer = meshRenderer;
            cachedTriangleArray = meshRenderer.TriangleArray();
            cachedTriangleIndices = Enumerable.Range(0, cachedTriangleArray.Length).ToArray();
            root = new BVHNodeMesh(cachedTriangleArray, cachedTriangleIndices, new ArrayRange(0, cachedTriangleArray.Length));
        }

        public void TryToOptimize()
        {
            root = root.OptimizeNode();
        }

        public float SAHCostForTree()
        {
            return root.CalculateTotalSAHCost();
        }

        public bool IntersectRay(Ray testRay, out float lambda, out int nIntersections)
        {
            lambda = float.MaxValue;
            nIntersections = 1;
            return root.bounds.IntersectRay(testRay) && root.IntersectRay(testRay, ref lambda, ref nIntersections);
        }

        public IEnumerable<Bounds> GetBounds()
        {
            yield return root.bounds;
            foreach (Bounds bounds in root.GetBounds())
            {
                yield return bounds;
            }
        }

        public BVHMeshGPUInstance GetGPUInstance()
        {
            return new BVHMeshGPUInstance
            {
                triangleIndices = cachedTriangleIndices,
                triangleArray = cachedTriangleArray,
                Nodes = GetAllNodes(),
            };
        }

        private BVHArrayNode[] GetAllNodes()
        {
            return root.GetAllNodes();
        }
    }
}