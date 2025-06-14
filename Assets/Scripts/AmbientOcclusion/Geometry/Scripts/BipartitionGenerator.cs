using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AmbientOcclusion.Geometry.Scripts
{
    public struct Bipartition
    {
        public static Bipartition emptyPartition = new();

        public int nItems => groupA.Count + groupB.Count;
        public List<int> groupA;
        public List<int> groupB;

        public Bipartition(List<int> groupA, List<int> groupB)
        {
            this.groupA = groupA;
            this.groupB = groupB;
        }
    }

    public static class BipartitionGenerator
    {
        public static IEnumerable<Bipartition> GetBipartitionForN(int n)
        {
            // TODO we could optimize this function more. but for now it's ok
            int total = 1 << n; // 2^n
            for (int mask = 1; mask < total - 1; mask++)
            {
                List<int> groupA = new List<int>();
                List<int> groupB = new List<int>();

                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        groupA.Add(i);
                    }
                    else
                    {
                        groupB.Add(i);
                    }
                }

                if (groupA[0] <= groupB[0])
                {
                    yield return new Bipartition(groupB, groupA);
                }
            }
        }
    }

    public class RandomBipartitionGenerator
    {
        private static bool[] GetRandomBitAssignments(int setSize)
        {
            if (setSize <= 0)
            {
                return Array.Empty<bool>();
            }

            int byteCount = (setSize + 7) / 8;
            byte[] randomBytes = new byte[byteCount];
            RandomNumberGenerator.Fill(randomBytes);

            bool[] assignments = new bool[setSize];
            for (int i = 0; i < setSize; i++)
            {
                int byteIndex = i / 8;
                int bitInByteIndex = i % 8;

                // Check if the bit is set
                if ((randomBytes[byteIndex] & (1 << bitInByteIndex)) != 0)
                {
                    assignments[i] = true;
                }
                else
                {
                    assignments[i] = false;
                }
            }

            return assignments;
        }

        public static Bipartition GetRandomBipartition(int setSize)
        {
            if (setSize <= 1)
            {
                throw new ArgumentException("Set size must be at least 2 to form meaningful non-empty bipartitions.",
                    nameof(setSize));
            }

            while (true)
            {
                bool[] assignments = GetRandomBitAssignments(setSize);

                List<int> groupA = new List<int>();
                List<int> groupB = new List<int>();

                for (int k = 0; k < setSize; k++)
                {
                    if (assignments[k]) // If true, assign to groupA
                    {
                        groupA.Add(k);
                    }
                    else // If false, assign to groupB
                    {
                        groupB.Add(k);
                    }
                }

                if (groupA.Count == 0 || groupB.Count == 0)
                {
                    continue;
                }

                if (groupA[0] < groupB[0])
                {
                    return new Bipartition(groupA, groupB);
                }

                return new Bipartition(groupB, groupA);
            }
        }

        public static Bipartition GetAlteredBipartition(Bipartition bipartition, int maxNumberOfSwamps)
        {
            Bipartition res = bipartition;

            for (int i = 0; i < RandomNumberGenerator.GetInt32(maxNumberOfSwamps); i++)
            {
                res = GetAlteredBipartition(res);
            }

            return res;
        }


        public static Bipartition GetAlteredBipartition(Bipartition bipartition)
        {
            List<int> newGroupA = new List<int>(bipartition.groupA);
            List<int> newGroupB = new List<int>(bipartition.groupB);

            if (bipartition.nItems <= 2)
            {
                return new Bipartition(newGroupA, newGroupB);
            }

            bool isGroupASource;

            if (newGroupA.Count == 1)
            {
                isGroupASource = false;
            }
            else if (newGroupB.Count == 1)
            {
                isGroupASource = true;
            }
            else
            {
                isGroupASource = RandomNumberGenerator.GetInt32(2) == 1;
            }

            List<int> source = isGroupASource ? newGroupA : newGroupB;
            List<int> target = isGroupASource ? newGroupB : newGroupA;

            int randomIndex = RandomNumberGenerator.GetInt32(source.Count);
            int itemToMove = source[randomIndex];

            source.RemoveAt(randomIndex);
            target.Add(itemToMove);

            return new Bipartition(newGroupA, newGroupB);
        }


        public static IEnumerable<Bipartition> GetAllAlteredBipartitionsOneMove(Bipartition partition)
        {
            // Move from groupA to groupB
            if (partition.groupA.Count > 1)
            {
                for (int i = 0; i < partition.groupA.Count; i++)
                {
                    int itemToMove = partition.groupA[i];

                    List<int> newSource = new List<int>(partition.groupA);
                    newSource.RemoveAt(i);

                    List<int> newTarget = new List<int>(partition.groupB) { itemToMove };

                    yield return new Bipartition(newSource, newTarget);
                }
            }

            // Move from groupB to groupA
            if (partition.groupB.Count > 1)
            {
                for (int i = 0; i < partition.groupB.Count; i++)
                {
                    int itemToMove = partition.groupB[i];

                    List<int> newSource = new List<int>(partition.groupB);
                    newSource.RemoveAt(i);

                    List<int> newTarget = new List<int>(partition.groupA) { itemToMove };

                    yield return new Bipartition(newTarget, newSource);
                }
            }
        }

        public static IEnumerable<Bipartition> GetAllAlteredBipartitionsSwap(Bipartition partition,
            HashSet<int> moveableNodes)
        {
            List<int> groupA = new List<int>(partition.groupA);
            List<int> groupB = new List<int>(partition.groupB);

            int countA = groupA.Count;
            int countB = groupB.Count;

            // For each element in A, swap it with each element in B
            for (int i = 0; i < countA; i++)
            {
                if (!moveableNodes.Contains(i))
                {
                    continue;
                }

                int aItem = groupA[i];
                for (int j = 0; j < countB; j++)
                {
                    if (!moveableNodes.Contains(j))
                    {
                        continue;
                    }

                    int bItem = groupB[j];

                    // Clone both lists, then swap at indices i and j
                    List<int> newA = new List<int>(groupA);
                    List<int> newB = new List<int>(groupB);

                    newA[i] = bItem;
                    newB[j] = aItem;

                    yield return new Bipartition(newA, newB);
                }
            }
        }
    }
}