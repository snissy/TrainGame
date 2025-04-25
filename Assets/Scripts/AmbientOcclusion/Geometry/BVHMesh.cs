using System.Collections.Generic;
using System.Linq;
using AmbientOcclusion.Geometry;
using UnityEngine;

namespace Matoya.Common.Geometry {

    public struct BVHArrayNode {
        public const int NInts = 4;
        public const int NBounds = 1;
        
        public int LeftNodeIndex;
        public int RightNodeIndex;
        public Bounds Bounds;
        public int leafIndexStart;
        public int leafIndexEnd;
    }
    public struct BVHMeshGPUInstance {
        // This is the geometry information
        public int[] triangleIndices;
        public Triangle[] triangleArray;
        public BVHArrayNode[] Nodes;
    }

    public readonly struct ArrayRange {
        
        public readonly int start;
        public readonly int end;
        public readonly int count;

        public ArrayRange(int start, int end) {
            this.start = start;
            this.end = end;
            this.count = end - start;
        }

        public void Split(out ArrayRange left, out ArrayRange right) {
            int midIndex = start + count / 2;
            left = new ArrayRange(start, midIndex);
            right = new ArrayRange(midIndex, end);
        }
    }

    internal class BVHLeafTriangle {
        
        public readonly Triangle[] globalTriangles;
        public readonly int[] triangleIndices;
        public readonly ArrayRange range;

        public BVHLeafTriangle(Triangle[] globalTriangles, int[] triangleIndices, ArrayRange range) {
            this.globalTriangles = globalTriangles;
            this.triangleIndices = triangleIndices;
            this.range = range;
        }

        public bool IntersectRay(Ray testRay, out float resultLambda) {
            resultLambda = float.MaxValue;
            bool hitOccurs = false;
            
            for(int i = range.start; i < range.end; i++) {
                bool intersectsRay = globalTriangles[triangleIndices[i]].IntersectRay(testRay, out float hitLambda);
                if (!intersectsRay) continue;
                hitOccurs = true;
                resultLambda = Mathf.Min(resultLambda, hitLambda);
            }
            
            return hitOccurs;
        }
    }

    internal class BVHNodeMesh {

        private const int LEAF_LIMIT = 4;

        private BVHLeafTriangle leaf;
        internal Bounds bounds;

        private BVHNodeMesh left;
        private BVHNodeMesh right;
        
        public BVHNodeMesh(Triangle[] globalTriangles, int[] triangleIndices, ArrayRange range) {
            
            Triangle firstTriangle = globalTriangles[triangleIndices[range.start]];
            bounds =  firstTriangle.bounds;
            
            for(int i = range.start; i < range.end; i++) {
                bounds.Encapsulate(globalTriangles[triangleIndices[i]].bounds);
            }

            if(range.count <= LEAF_LIMIT) {
                leaf = new BVHLeafTriangle(globalTriangles, triangleIndices, range);
                return;
            }
            
            BVHUtils.SplitVolumeSah(bounds,
                globalTriangles,
                triangleIndices,
                range,
                8,
                out ArrayRange leftRange,
                out ArrayRange rightRange);
        
            left = new BVHNodeMesh(globalTriangles, triangleIndices, leftRange);
            right = new BVHNodeMesh(globalTriangles, triangleIndices, rightRange);
        }
        

        public bool IntersectRay(Ray testRay, ref float minLambda) {

            if(leaf != null) {
                if(!leaf.IntersectRay(testRay, out float leafLambda)) {
                    return false;
                }
                if(minLambda < leafLambda) {
                    return true;
                }
                minLambda = leafLambda;
                return true;
            }

            BVHNodeMesh first = left;
            BVHNodeMesh second = right;

            if(!left.bounds.IntersectRay(testRay, out float firstHit)) {
                firstHit = float.MaxValue;
            }
            if(!right.bounds.IntersectRay(testRay, out float secondHit)) {
                secondHit = float.MaxValue;
            }

            if(secondHit < firstHit) {
                (first, second) = (right, left);
                (firstHit, secondHit) = (secondHit, firstHit);
            }

            bool firstIntersects = firstHit < minLambda && first.IntersectRay(testRay, ref minLambda);
            bool secondIntersects = secondHit < minLambda && second.IntersectRay(testRay, ref minLambda);

            return firstIntersects || secondIntersects;
        }

        public IEnumerable<Bounds> GetBounds() {
            yield return bounds;
            if(leaf != null) {
                yield break;
            }
            foreach(Bounds bound in left.GetBounds()) {
                yield return bound;
            }
            foreach(Bounds bound in right.GetBounds()) {
                yield return bound;
            }
        }

        public BVHArrayNode[] GetAllNodes() {
            List<BVHArrayNode> result = new List<BVHArrayNode>();
            Queue<BVHNodeMesh> searchStack = new Queue<BVHNodeMesh>();
            searchStack.Enqueue(this);

            int resultCount = 0;
            
            while (searchStack.Count > 0 ) {

                BVHNodeMesh bvhNode = searchStack.Dequeue();

                BVHArrayNode newBvhArrayNode = new BVHArrayNode {
                    Bounds = bvhNode.bounds
                };
                
                if (bvhNode.leaf != null) {
                    newBvhArrayNode.leafIndexStart = bvhNode.leaf.range.start;
                    newBvhArrayNode.leafIndexEnd = bvhNode.leaf.range.end;
                    result.Add(newBvhArrayNode);
                    continue;
                }

                newBvhArrayNode.LeftNodeIndex = resultCount + 1;
                newBvhArrayNode.RightNodeIndex = resultCount + 2;
                
                resultCount += 2;
                result.Add(newBvhArrayNode);
                
                searchStack.Enqueue(bvhNode.left);
                searchStack.Enqueue(bvhNode.right);
            }
            
            return result.ToArray();
        }
    }

    public class BVHMesh {

        private readonly BVHNodeMesh root;
        private readonly MeshRenderer renderer;
        private readonly Triangle[] cachedTriangleArray;
        private readonly int[] cachedTriangleIndices;

        public MeshRenderer Renderer => renderer;

        public BVHMesh(MeshRenderer meshRenderer) {
            renderer = meshRenderer;
            cachedTriangleArray = meshRenderer.TriangleArray();
            cachedTriangleIndices = Enumerable.Range(0, cachedTriangleArray.Length).ToArray();
            root = new BVHNodeMesh(cachedTriangleArray, cachedTriangleIndices, new ArrayRange(0, cachedTriangleArray.Length));
        }
 
        public bool IntersectRay(Ray testRay, out float lambda) {
            lambda = float.MaxValue;
            return root.bounds.IntersectRay(testRay) && root.IntersectRay(testRay, ref lambda);
        }

        public IEnumerable<Bounds> GetBounds() {
            yield return root.bounds;
            foreach(Bounds bounds in root.GetBounds()) {
                yield return bounds;
            }
        }

        public BVHMeshGPUInstance GetGPUInstance() {
            return new BVHMeshGPUInstance {
                triangleIndices = this.cachedTriangleIndices,
                triangleArray = this.cachedTriangleArray,
                Nodes = GetAllNodes(),
            };
        }

        private BVHArrayNode[] GetAllNodes() {
            return root.GetAllNodes();
        }
    }
}
