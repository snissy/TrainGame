using System;
using System.Collections.Generic;
using UnityEngine;
namespace Matoya.Common.Geometry {
    internal class BVHSceneLeaf {
        private BVHMesh value;
        public BVHSceneLeaf(MeshRenderer value) {
            this.value = new BVHMesh(value);
        }
        public BVHMesh Value => value;

        public bool IntersectRay(Ray testRay, out float resultLambda) {
            return value.IntersectRay(testRay, out resultLambda);
        }
    }

    internal class BVHSceneNode {
        internal Bounds bounds;
        private BVHSceneLeaf sceneLeaf;

        private BVHSceneNode left;
        private BVHSceneNode right;

        public BVHSceneNode(List<MeshRenderer> meshRenderers) {
            bounds =  meshRenderers[0].bounds;

            for(int i = 1; i < meshRenderers.Count; i++) {
                bounds.Encapsulate(meshRenderers[i].bounds);
            }

            if(meshRenderers.Count == 1) {
                sceneLeaf = new BVHSceneLeaf(meshRenderers[0]);
                return;
            }

            BVHUtils.SplitVolumeSah(meshRenderers, bounds, 32, out List<MeshRenderer> leftList, out List<MeshRenderer> rightList);

            left = new BVHSceneNode(leftList);
            right = new BVHSceneNode(rightList);
        }

        public bool IntersectRay(Ray testRay, ref float minLambda, ref MeshRenderer meshRenderer, ref int nTestRay) {

            if(sceneLeaf != null) {
                nTestRay++;
                if(!sceneLeaf.IntersectRay(testRay, out float meshLambda)) {
                    return false;
                }
                if(minLambda < meshLambda) {
                    return true;
                }
                minLambda = meshLambda;
                meshRenderer = sceneLeaf.Value.Renderer;
                return true;
            }

            BVHSceneNode first = left;
            BVHSceneNode second = right;

            nTestRay++;
            if(!left.bounds.IntersectRay(testRay, out float firstHit)) {
                firstHit = float.MaxValue;
            }
            nTestRay++;
            if(!right.bounds.IntersectRay(testRay, out float secondHit)) {
                secondHit = float.MaxValue;
            }

            if(secondHit < firstHit) {
                (first, second) = (right, left);
                (firstHit, secondHit) = (secondHit, firstHit);
            }

            bool firstIntersects = firstHit < minLambda && first.IntersectRay(testRay, ref minLambda, ref meshRenderer, ref nTestRay);
            bool secondIntersects = secondHit < minLambda && second.IntersectRay(testRay, ref minLambda, ref meshRenderer, ref nTestRay);

            return firstIntersects || secondIntersects;
        }

        public IEnumerable<Bounds> GetBounds() {
            yield return bounds;
            if(sceneLeaf != null) {
                yield break;
            }
            foreach(Bounds bound in left.GetBounds()) {
                yield return bound;
            }
            foreach(Bounds bound in right.GetBounds()) {
                yield return bound;
            }
        }
    }

    public class BVHScene {

        private BVHSceneNode root;

        public BVHScene(List<MeshRenderer> meshRenderer) {
            root = new BVHSceneNode(meshRenderer);
        }

        public bool IntersectRay(Ray testRay, out float lambda, out MeshRenderer meshRenderer, out int nTestRay) {
            nTestRay = 1;
            lambda = float.MaxValue;
            meshRenderer = null;
            return root.bounds.IntersectRay(testRay) && root.IntersectRay(testRay, ref lambda, ref meshRenderer, ref nTestRay);
        }

        public IEnumerable<Bounds> GetBounds() {
            yield return root.bounds;
            foreach(Bounds bounds in root.GetBounds()) {
                yield return bounds;
            }
        }
    }

    public static class BVHUtils {

        struct SahBin {
            public int count;
            public Bounds bounds;

            public void AddPrimitive(Bounds newBound) {
                if(count == 0) {
                    bounds = newBound;
                }
                else {
                    bounds.Encapsulate(newBound);
                }
                count++;
            }

            public void MergeBin(SahBin newBin) {
                if(count == 0) {
                    if(newBin.count > 0) {
                        bounds = newBin.bounds;
                    }
                }
                else {
                    if(newBin.count > 0) {
                        bounds.Encapsulate(newBin.bounds);
                    }
                }
                count += newBin.count;
            }
        }

        private static readonly Vector3[] axes = { new(1, 0, 0), new(0, 1, 0), new(0, 0, 1) };

        public static void SplitVolume(List<MeshRenderer> visuals, out List<MeshRenderer> leftList, out List<MeshRenderer> rightList) {

            int nItems = visuals.Count;
            int midIndex = Mathf.FloorToInt(nItems / 2.0f);

            leftList = new();

            rightList = new();

            for(int i = 0; i < midIndex; i++) {
                MeshRenderer visual = visuals[i];
                leftList.Add(visual);
            }

            for(int i = midIndex; i < nItems; i++) {
                MeshRenderer visual = visuals[i];
                rightList.Add(visual);
            }
        }

        public static void SortOnBiggestAxis(Bounds rootVolume, List<MeshRenderer> visuals) {

            float biggestMag = rootVolume.extents.x;
            Vector3 biggestAxis = new(1, 0, 0);

            float yValue = rootVolume.extents.y;
            float zValue = rootVolume.extents.z;

            if(yValue > biggestMag) {
                biggestMag = yValue;
                biggestAxis = new(0, 1, 0);
            }
            if(zValue > biggestMag) {
                biggestAxis = new(0, 0, 1);
            }

            visuals.Sort((b1, b2) => {
                float b1Coord = Vector3.Dot(b1.bounds.center, biggestAxis);
                float b2Coord = Vector3.Dot(b2.bounds.center, biggestAxis);
                return b1Coord.CompareTo(b2Coord);
            });
        }
        
        public class TriangleComparer : IComparer<int> {
            
            private readonly Triangle[] triangles;
            private readonly Vector3 biggestAxis;

            public TriangleComparer(Triangle[] triangles, Vector3 biggestAxis) {
                this.triangles = triangles;
                this.biggestAxis = biggestAxis;
            }

            public int Compare(int b1, int b2) {
                float b1Coord = Vector3.Dot(triangles[b1].bounds.center, biggestAxis);
                float b2Coord = Vector3.Dot(triangles[b2].bounds.center, biggestAxis);
                return b1Coord.CompareTo(b2Coord);
            }
        }

        public static void SortOnBiggestAxis(Bounds rootVolume, Triangle[] triangles, int[] triangleIndices, ArrayRange range) {

            float biggestMag = rootVolume.extents.x;
            Vector3 biggestAxis = new(1, 0, 0);

            float yValue = rootVolume.extents.y;
            float zValue = rootVolume.extents.z;

            if (yValue > biggestMag) {
                biggestMag = yValue;
                biggestAxis = new(0, 1, 0);
            }

            if (zValue > biggestMag) {
                biggestAxis = new(0, 0, 1);
            }
            
            Array.Sort(triangleIndices, range.start, range.count, new TriangleComparer(triangles, biggestAxis));
        }


        public static Vector3[] CalculateWorldBoxCorners(Bounds bounds) {

            Vector3 extent = bounds.extents;
            Vector3 center = bounds.center;

            Vector3 xComponent = Vector3.Project(extent, Vector3.right);
            Vector3 yComponent = Vector3.Project(extent, Vector3.up);
            Vector3 zComponent = Vector3.Project(extent, Vector3.forward);

            Vector3 v1 = center + extent;
            Vector3 v2 = center - xComponent + yComponent + zComponent;
            Vector3 v3 = center - xComponent + yComponent - zComponent;
            Vector3 v4 = center + xComponent + yComponent - zComponent;

            Vector3 v5 = center + xComponent - yComponent + zComponent;
            Vector3 v6 = center - xComponent - yComponent + zComponent;
            Vector3 v7 = center - xComponent - yComponent - zComponent;
            Vector3 v8 = center + xComponent - yComponent - zComponent;

            return new[] { v1, v2, v3, v4, v5, v6, v7, v8 };
        }

        public static void SplitVolumeSah(List<MeshRenderer> visuals, Bounds groupBounds, int nBins, out List<MeshRenderer> left, out List<MeshRenderer> right) {

            left = new();
            right = new();

            float bestGlobalPartitionCost = float.MaxValue;
            int bestGlobalPartitionIndex = 0;
            Vector3 bestGlobalAxis = axes[0];

            float groupBoundsSurfaceArea = groupBounds.SurfaceArea();

            float axisLength;
            float axisStart;
            float binSize;

            foreach(Vector3 axis in axes) {
                SahBin[] bins = new SahBin[nBins];
                axisLength = Vector3.Dot(axis, groupBounds.size);
                axisStart = Vector3.Dot(axis, groupBounds.center) - axisLength * 0.5f;
                binSize = axisLength / nBins;

                foreach(MeshRenderer visual in visuals) {
                    Bounds visualBounds = visual.bounds;
                    Vector3 visualCentroid = visualBounds.center;
                    int bIndex = Mathf.FloorToInt((Vector3.Dot(axis, visualCentroid) - axisStart) / binSize);
                    bins[bIndex].AddPrimitive(visualBounds);
                }

                int bestCostIndex = 0;
                float bestCostOnAxis = float.MaxValue;
                for(int i = 0; i < nBins - 1; i++) {
                    SahBin b0 = new(), b1 = new();
                    for(int j = 0; j <= i; j++) {
                        b0.MergeBin(bins[j]);
                    }
                    for(int j = i + 1; j < nBins; j++) {
                        b1.MergeBin(bins[j]);
                    }
                    float cost = (b0.count * b0.bounds.SurfaceArea() + b1.count * b1.bounds.SurfaceArea()) / groupBoundsSurfaceArea;
                    if(cost < bestCostOnAxis) {
                        bestCostIndex = i;
                        bestCostOnAxis = cost;
                    }
                }

                if(bestCostOnAxis < bestGlobalPartitionCost) {
                    bestGlobalPartitionCost = bestCostOnAxis;
                    bestGlobalPartitionIndex = bestCostIndex;
                    bestGlobalAxis = axis;
                }
            }

            axisLength = Vector3.Dot(bestGlobalAxis, groupBounds.size);
            axisStart = Vector3.Dot(bestGlobalAxis, groupBounds.center) - axisLength * 0.5f;
            binSize = axisLength / nBins;

            foreach(MeshRenderer visual in visuals) {
                Bounds visualBounds = visual.bounds;
                Vector3 visualCentroid = visualBounds.center;
                int bIndex = Mathf.FloorToInt((Vector3.Dot(bestGlobalAxis, visualCentroid) - axisStart) / binSize);
                if(bIndex <= bestGlobalPartitionIndex) {
                    left.Add(visual);
                }
                else {
                    right.Add(visual);
                }
            }

            // check if we have empty list.
            if(right.Count != 0 && left.Count != 0) {
                return;
            }

            SortOnBiggestAxis(groupBounds, visuals);
            SplitVolume(visuals, out List<MeshRenderer> leftList, out List<MeshRenderer> rightList);
            left = leftList;
            right = rightList;
        }

        private static IEnumerable<Triangle> GetTriangleEnumerable(Triangle[] triangles, int[] triangleIndices, ArrayRange range) {
            for (int i = range.start; i < range.end; i++) {
                yield return triangles[triangleIndices[i]];
            }
        }

        public static void SplitVolumeSah(Bounds groupBounds, Triangle[] triangles, int[] triangleIndices, ArrayRange range, int nBins, out ArrayRange left, out ArrayRange right) {
            
            float bestGlobalPartitionCost = float.MaxValue;
            int bestGlobalPartitionIndex = 0;
            Vector3 bestGlobalAxis = axes[0];

            float groupBoundsSurfaceArea = groupBounds.SurfaceArea();

            float axisLength;
            float axisStart;
            float binSize;

            foreach(Vector3 axis in axes) {
                SahBin[] bins = new SahBin[nBins];
                axisLength = Vector3.Dot(axis, groupBounds.size);
                axisStart = Vector3.Dot(axis, groupBounds.center) - axisLength * 0.5f;

                binSize = 1e-9f + (axisLength / nBins);

                foreach(Triangle visual in GetTriangleEnumerable(triangles, triangleIndices, range)) {
                    Bounds visualBounds = visual.bounds;
                    Vector3 visualCentroid = visualBounds.center;
                    float t = Vector3.Dot(axis, visualCentroid) - axisStart;
                    int bIndex = Mathf.FloorToInt(Mathf.Clamp01(t/binSize));
                    bins[bIndex].AddPrimitive(visualBounds);
                }

                int bestCostIndex = 0;
                float bestCostOnAxis = float.MaxValue;
                for(int i = 0; i < nBins - 1; i++) {
                    SahBin b0 = new(), b1 = new();
                    for(int j = 0; j <= i; j++) {
                        b0.MergeBin(bins[j]);
                    }
                    for(int j = i + 1; j < nBins; j++) {
                        b1.MergeBin(bins[j]);
                    }
                    float cost = (b0.count * b0.bounds.SurfaceArea() + b1.count * b1.bounds.SurfaceArea()) / groupBoundsSurfaceArea;
                    if(cost < bestCostOnAxis) {
                        bestCostIndex = i;
                        bestCostOnAxis = cost;
                    }
                }

                if(bestCostOnAxis < bestGlobalPartitionCost) {
                    bestGlobalPartitionCost = bestCostOnAxis;
                    bestGlobalPartitionIndex = bestCostIndex;
                    bestGlobalAxis = axis;
                }
            }

            axisLength = Vector3.Dot(bestGlobalAxis, groupBounds.size);
            axisStart = Vector3.Dot(bestGlobalAxis, groupBounds.center) - axisLength * 0.5f;
            binSize = 1e-9f + (axisLength / nBins);
            
            left = range;
            right = new ArrayRange(range.end, range.end);

            int leftPivot = range.start;
            int rightPivot = range.end - 1;
            
            Vector3 leftCenter = triangles[triangleIndices[leftPivot]].bounds.center;
            Vector3 rightCenter = triangles[triangleIndices[rightPivot]].bounds.center;
                
            bool leftValue = Mathf.FloorToInt((Vector3.Dot(bestGlobalAxis, leftCenter) - axisStart) / binSize) <= bestGlobalPartitionIndex;
            bool rightValue = Mathf.FloorToInt((Vector3.Dot(bestGlobalAxis, rightCenter) - axisStart) / binSize) <= bestGlobalPartitionIndex;

            // This is sorting stuff right, haha I dont remember. 
            while (leftPivot < rightPivot) {

                while (leftValue && leftPivot < rightPivot) {
                    leftPivot++;
                    leftCenter = triangles[triangleIndices[leftPivot]].bounds.center;
                    leftValue = Mathf.FloorToInt((Vector3.Dot(bestGlobalAxis, leftCenter) - axisStart) / binSize) <= bestGlobalPartitionIndex;
                }
                
                if (!rightValue && leftPivot < rightPivot) {
                    rightPivot--;
                    rightCenter = triangles[triangleIndices[rightPivot]].bounds.center;
                    rightValue = Mathf.FloorToInt((Vector3.Dot(bestGlobalAxis, rightCenter) - axisStart) / binSize) <= bestGlobalPartitionIndex;
                }

                if (!leftValue && rightValue) {
                    (triangleIndices[leftPivot], triangleIndices[rightPivot]) = (triangleIndices[rightPivot], triangleIndices[leftPivot]);
                    leftPivot++;
                    rightPivot--;
                }
            }
            
            left = new ArrayRange(range.start, leftPivot);
            right = new ArrayRange(leftPivot, range.end);
            
            if(left.count == 0 || right.count == 0) {
                SortOnBiggestAxis(groupBounds, triangles, triangleIndices, range);
                range.Split(out ArrayRange leftRange, out ArrayRange rightRange);
                left = leftRange;
                right = rightRange;
            }
        }
    }
}
