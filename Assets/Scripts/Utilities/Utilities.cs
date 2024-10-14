using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using static Unity.Mathematics.math;
using ECSTest.Components;
using Unity.Jobs.LowLevel.Unsafe;
using System;
using System.Globalization;

namespace CardTD.Utilities
{
    public static class Utilities
    {
        static float spare;
        static bool hasSpare;

        public static float NextGaussian(float mean, float stdDev)
        {
            return mean + GetNormalDistribution() * stdDev;
        }

        private static System.Random rng = new System.Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
        public static List<T> GetFlagValues<T>(T f) where T : Enum
        {
            List<T> flags = new List<T>();
            foreach (T flag in Enum.GetValues(typeof(T)))
            {
                if (f.HasFlag(flag))
                    flags.Add(flag);
            }
            return flags;
        }

        public static float GetNormalDistribution()
        {
            if (hasSpare)
            {
                hasSpare = false;
                return spare;
            }

            float v1, v2, s;
            do
            {
                v1 = 2 * Random.value - 1;
                v2 = 2 * Random.value - 1;
                s = v1 * v1 + v2 * v2;
            } while (s >= 1 || s == 0);

            s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);
            spare = v2 * s;
            hasSpare = true;
            return v1 * s;
        }

        public static float3 ToFloat3(this float2 source) => new float3(source, 0);
        
        private static readonly Dictionary<int, string> shortcuts = new()
        {
        {0, string.Empty},{3, "K"}, {6, "M"}, {9, "B"}, {12, "T"},

        {15, "aa"}, {18, "ab"}, {21, "ac"}, {24, "ad"}, {27, "ae"}, {30, "af"}, {33, "ag"}, {36, "ah"}, {39, "ai"},
        {42, "aj"}, {45, "ak"}, {48, "al"}, {51, "am"}, {54, "an"}, {57, "ao"}, {60, "ap"}, {63, "aq"}, {66, "ar"},
        {69, "as"}, {72, "at"}, {75, "au"}, {78, "av"}, {81, "aw"}, {84, "ax"}, {87, "ay"}, {90, "az"},

        {93,  "ba"}, {96,  "bb"}, {99,  "bc"}, {102, "bd"}, {105, "be"}, {108, "bf"}, {111, "bg"}, {114, "bh"},
        {117, "bi"}, {120, "bj"}, {123, "bk"}, {126, "bl"}, {129, "bm"}, {132, "bn"}, {135, "bo"}, {138, "bp"},
        {141, "bq"}, {144, "br"}, {147, "bs"}, {150, "bt"}, {153, "bu"}, {156, "bv"}, {159, "bw"}, {162, "bx"},
        {165, "by"}, {168, "bz"},

        {171, "ca"}, {174, "cb"}, {177, "cc"}, {180, "cd"}, {183, "ce"}, {186, "cf"}, {189, "cg"}, {192, "ch"},
        {195, "ci"}, {198, "cj"}, {201, "ck"}, {204, "cl"}, {207, "cm"}, {210, "cn"}, {213, "co"}, {216, "cp"},
        {219, "cq"}, {222, "cr"}, {225, "cs"}, {228, "ct"}, {231, "cu"}, {234, "cv"}, {237, "cw"}, {240, "cx"},
        {243, "cy"}, {246, "cz"},

        {249, "da"}, {252, "db"}, {255, "dc"}, {258, "dd"}, {261, "de"}, {264, "df"}, {267, "dg"}, {270, "dh"},
        {273, "di"}, {276, "dj"}, {279, "dk"}, {282, "dl"}, {285, "dm"}, {288, "dn"}, {291, "do"}, {294, "dp"},
        {297, "dq"}, {300, "dr"}, {303, "ds"},
        };
        
        public static string ToStringBigValue(this int value, bool separately = false)
        {
            if (value.Equals(0))
                return "0";

            int power = (int)Math.Log10(value);
            double result = (int)(value / Math.Pow(10, power - 2));
            int numen = power / 3;
            result *= (Math.Pow(10, power - 2) / Math.Pow(10, numen * 3));
            return $"{result.ToString(CultureInfo.InvariantCulture)}{(separately ? " " : string.Empty)}{shortcuts[numen * 3]}";
        }
        
        public static string ToStringBigValue(this float value, bool separately = false)
        {
            if (value.Equals(0))
                return "0";

            if (value < 1000)
                return $"{value:0.0}";
            
            int power = (int)Math.Log10(value);
            float result = (int)(value / Math.Pow(10, power - 2));
            int numen = power / 3;
            result *= (Mathf.Pow(10, power - 2) / Mathf.Pow(10, numen * 3));
            return $"{result.ToString(CultureInfo.InvariantCulture)}{(separately ? " " : string.Empty)}{shortcuts[numen * 3]}";
        }
        
        public static string ToStringBigValue(in double value, bool separately = false)
        {
            if (value.Equals(0))
                return "0";

            if (double.IsInfinity(value))
                return "Inf";

            int power = (int)Math.Log10(value);
            double result = (int)(value / Math.Pow(10, power - 2));
            int numen = power / 3;
            result *= (Math.Pow(10, power - 2) / Math.Pow(10, numen * 3));
            return $"{result}{(separately ? " " : string.Empty)}{shortcuts[numen * 3]}".ToString(CultureInfo.InvariantCulture);
        }


        public static int TowerIdToInt(AllEnums.TowerId towerId)
        {
            int index = 0;
            while (((int)towerId & 1 << (index -1)) == 0)
                index++;

            //return index;
            return index >8? index-1:index;//where is AllEnums.TowerId 1 << 7?? del this after fix
        }

        //public static float3 GetEulerAngle(this float3 dir) => new float3(0, 0, atan2(dir.y, dir.x));
        //public static float3 GetEulerAngle(this float2 dir) => new float3(0, 0, atan2(dir.y, dir.x));
        public static void GetNormalDistribution(ref Unity.Mathematics.Random random, out float result1, out float result2)
        {
            float v1, v2, s;
            do
            {
                v1 = 2 * random.NextFloat() - 1;
                v2 = 2 * random.NextFloat() - 1;
                s = v1 * v1 + v2 * v2;
            } while (s >= 1 || s == 0);

            s = sqrt((-2.0f * log(s)) / s);

            result1 = v1 * s;
            result2 = v2 * s;
        }

        public static void GetGaussian(ref Unity.Mathematics.Random random, float mean, float stdDev, out float result1, out float result2)
        {
            GetNormalDistribution(ref random, out result1, out result2);

            result1 = mean + result1 * stdDev;
            result2 = mean + result2 * stdDev;
        }

        public static float2 GetRotated(this float2 dir, float angle)
        {
            float cos = math.cos(angle);
            float sin = math.sin(angle);
            return new float2(dir.x * cos - dir.y * sin, dir.x * sin + dir.y * cos);
        }

        /// <summary>
        /// In Radians
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SignedAngleBetween(float2 from, float2 to) => atan2(to.y * from.x - to.x * from.y, to.x * from.x + to.y * from.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CrossProduct(float2 a, float2 b) => a.x * b.y - a.y * b.x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 GetNormal(this float2 line) => new float2(line.y, -line.x);

        public static float GetMagnitude(this float2 source) => source.x * source.x + source.y * source.y;

        public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(this IDictionary<TKey, TValue> source)
        {
            var dictionary = new Dictionary<TValue, TKey>();
            foreach (var entry in source)
            {
                if (!dictionary.ContainsKey(entry.Value))
                    dictionary.Add(entry.Value, entry.Key);
            }

            return dictionary;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeft(float2 start, float2 line, float2 point) => line.x * (point.y - start.y) - line.y * (point.x - start.x) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(this float2 vector) => vector.x == 0 && vector.y == 0;

        public static Quaternion Direction2DToQuaternion(float2 direction) => Quaternion.LookRotation(direction.ToFloat3(), new float3(0, 0, 1));

        public static float2 GetRandomDirection(RandomComponent randomComponent)
        {
            var random = randomComponent.GetRandom(JobsUtility.ThreadIndex);
            float2 randomDirection = new float2(random.NextFloat(-1, 1), random.NextFloat(-1, 1));
            if (randomDirection.Equals(0))
                randomDirection = 1;

            randomComponent.SetRandom(random, JobsUtility.ThreadIndex);

            return math.normalize(randomDirection);
        }

        public static GameObject GetRootObject(string name)
        {
            GameObject go = GameObject.Find(name);

            if (go == null)
                go = new GameObject(name);

            return go;
        }


        /// <summary>
        /// This is from math forum so I have no idea how it works
        /// </summary>
        /// <param name="start"></param>
        /// <param name="offset"></param>
        /// <param name="endPoint"></param>
        /// <param name="t"> normalized arc part 0.3 => 30% of arc  (0;1) </param>
        /// <returns></returns>
        public static float GetBezierLength(float2 start, float2 offset, float2 endPoint, float t = 1) // get arclength from parameter t=<0,1>
        {
            float bigA, bigB, bigC, b, c, u, k, length;
            float2 aPoint = start - 2 * offset + endPoint;
            float2 bPoint = 2 * offset - 2 * start;
            bigA = 4 * dot(aPoint, aPoint);
            bigB = 4 * dot(aPoint, bPoint);
            bigC = dot(bPoint, bPoint);

            b = bigB / (2 * bigA);
            c = bigC / bigA;
            u = t + b;
            k = c - (b * b);
            length = 0.5f * sqrt(bigA) *
                     ((u * sqrt((u * u) + k))
                      - (b * sqrt((b * b) + k))
                      + (k * log(abs((u + sqrt((u * u) + k)) / (b + sqrt((b * b) + k)))))
                     );
            return length;
        }

        public static T GetRandomValue<T>(this IList<T> list)
        {
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static TKey GetKeyForValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue val)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in dictionary)
            {
                if (kvp.Value.Equals(val))
                    return kvp.Key;
            }

            return default(TKey); // or throw an appropriate exception for not having found the key
        }

        public static TKey GetKeyByIndex<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, int index)
        {
            if (dictionary.Count >= index || index < 0)
                return default(TKey);
            int counter = 0;
            foreach (KeyValuePair<TKey, TValue> kvp in dictionary)
            {
                if (counter == index)
                    return kvp.Key;
                counter++;
            }

            return default(TKey);
        }

        public static float GetLerpedValue(float lowerBound, float topBound, float lowerValue, float topValue, float input)
        {
            float t = math.clamp(math.unlerp(lowerBound, topBound, input), 0, 1);
            return math.lerp(lowerValue, topValue, t);
        }
        
        public static int2 GetRandomPosition(GridPosition[] gridPosition)
        {
            int size = 0;
            for (int i = 0; i < gridPosition.Length; i++)
            {
                size += gridPosition[i].Area;
            }

            int randomSize = UnityEngine.Random.Range(0, size);

            for (int i = 0; i < gridPosition.Length; i++)
            {
                randomSize -= gridPosition[i].Area;
                if (randomSize <= 0)
                {
                    int x = UnityEngine.Random.Range(0, gridPosition[i].GridSize.x);
                    int y = UnityEngine.Random.Range(0, gridPosition[i].GridSize.y);
                    return new int2(x, y) + gridPosition[i].GridPos;
                }
            }

            throw new Exception($"{randomSize > 0}");
        }
    }
}