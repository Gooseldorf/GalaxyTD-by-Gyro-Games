using CardTD.Utilities;
using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Data.Managers
{
    public class MainMenuDaily
    {
        private const string dayPrefKey = nameof(dayPrefKey);
        private const string rewardPrefKey = nameof(rewardPrefKey);
        private const string lastDirectiveRewardId = nameof(lastDirectiveRewardId);

        private static List<DailyReward> dailyRewards = new()
        {
            new DailyReward(dailyRewardType: DailyRewardType.Soft, value: .5f),
            new DailyReward(dailyRewardType: DailyRewardType.Hard, value: 5),
            new DailyReward(dailyRewardType: DailyRewardType.Ticket, value: 2f),
            new DailyReward(dailyRewardType: DailyRewardType.Scrap, value: .2f),
            new DailyReward(dailyRewardType: DailyRewardType.Soft, value: 1.5f),
            new DailyReward(dailyRewardType: DailyRewardType.Hard, value: 15),
            new DailyReward(dailyRewardType: DailyRewardType.RandomDirective, value: 1f),
        };
        #region beforeHardMode
        //private static List<DailyReward> dailyRewardsBeforeHardMode = new()
        //{
        //    new DailyReward(dailyRewardType: DailyRewardType.Soft, value: .5f),
        //    new DailyReward(dailyRewardType: DailyRewardType.Hard, value: 5),
        //    new DailyReward(dailyRewardType: DailyRewardType.Soft, value: 1f),
        //    new DailyReward(dailyRewardType: DailyRewardType.Soft, value: 1.5f),
        //    new DailyReward(dailyRewardType: DailyRewardType.Hard, value: 10),
        //    new DailyReward(dailyRewardType: DailyRewardType.Hard, value: 15),
        //    new DailyReward(dailyRewardType: DailyRewardType.RandomDirective, value: 1f),
        //};

        //private static List<DailyReward> tempDailyRewards;

        //private static void CheckIsHardmodeAwailableNow()
        //{
        //    bool isHardModeUnlocked = DataManager.Instance.GameData.Stars.Count >= DataManager.Instance.HardModeMissionCountThreshold;
        //    tempDailyRewards = isHardModeUnlocked ? dailyRewards : dailyRewardsBeforeHardMode;
        //}

        private static bool isLastRewardHard;

        #endregion
        public static DailyReward GetLastReward
        {
            get
            {
                //if (tempDailyRewards == null)
                //    CheckIsHardmodeAwailableNow();

                try
                {
                    if (CountDayBetween == 0)
                        return CountTakeReward == 1 && !isLastRewardHard ? dailyRewards[1] : dailyRewards[CountTakeReward - 1];

                    if (CountDayBetween > 1000)
                        return dailyRewards[1];

                    //if (CountTakeReward == 1)
                    //    return isLastRewardHard ? dailyRewards[0] : dailyRewards[1];

                    return dailyRewards[CountTakeReward];
                }
                catch (Exception e)
                {
                    Debug.LogError($"ex in last reward: {e}");
                    return dailyRewards[0];
                }
            }
        }

        public static int CountTakeReward
        {
            get => PlayerPrefs.GetInt(rewardPrefKey, 0);
            set
            {
                PlayerPrefs.SetInt(rewardPrefKey, value);
                PlayerPrefs.Save();
            }
        }

        private static DateTime lastDay
        {
            get => new(long.Parse(PlayerPrefs.GetString(dayPrefKey, "0")));
            set
            {
                PlayerPrefs.SetString(dayPrefKey, $"{value.Ticks}");
                PlayerPrefs.Save();
            }
        }

        private static DateTime day => DateTime.Now;
        public static int CountDayBetween => (day.Date - lastDay.Date).Days;

        public static List<DailyRewardItem> GetRewards()
        {
            List<DailyRewardItem> result = new();

            //if first take daily reward
            if (CountDayBetween > 1000)
            {
                //CheckIsHardmodeAwailableNow();
                result.Add(new DailyRewardItem() { Reward = dailyRewards[0], StatusType = DailyRewardStatusType.ReadyToTake });
                result.Add(new DailyRewardItem() { Reward = dailyRewards[1], StatusType = DailyRewardStatusType.ReadyToTake });
                for (int i = 2; i < dailyRewards.Count; i++)
                    result.Add(new DailyRewardItem() { Reward = dailyRewards[i], StatusType = DailyRewardStatusType.NotReady });
                return result;
            }

            //if take one reward on first day
            if (CountTakeReward == 1)
            {
                //CheckIsHardmodeAwailableNow();
                result.Add(new DailyRewardItem() { Reward = dailyRewards[0], StatusType = isLastRewardHard ? DailyRewardStatusType.ReadyToTake : DailyRewardStatusType.Taken });
                result.Add(new DailyRewardItem() { Reward = dailyRewards[1], StatusType = isLastRewardHard ? DailyRewardStatusType.Taken : DailyRewardStatusType.ReadyToTake });

                for (int i = 2; i < dailyRewards.Count; i++)
                    result.Add(new DailyRewardItem() { Reward = dailyRewards[i], StatusType = DailyRewardStatusType.NotReady });
                return result;
            }

            if (CountDayBetween > 1 || (CountTakeReward >= dailyRewards.Count && CountDayBetween != 0))
            {
                CountTakeReward = 0;
                //CheckIsHardmodeAwailableNow();
            }

            for (int i = 0; i < dailyRewards.Count; i++)
            {
                DailyRewardItem dailyRewardItem = new();
                dailyRewardItem.Reward = dailyRewards[i];
                dailyRewardItem.StatusType = (i < CountTakeReward) ? DailyRewardStatusType.Taken :
                    (i == CountTakeReward && CountDayBetween != 0) ? DailyRewardStatusType.ReadyToTake : DailyRewardStatusType.NotReady;
                result.Add(dailyRewardItem);
            }

            return result;
        }

        public static void TakeReward(DailyReward reward, out WeaponPart directiveReward)
        {
            DataManager.Instance.GameData.TakeReward(reward, out directiveReward);
            lastDay = day;
            CountTakeReward += 1;
            PlayerPrefs.SetString(lastDirectiveRewardId, directiveReward == null ? String.Empty : directiveReward.SerializedID);
            Messenger.Broadcast(GameEvents.GetDailyReward, MessengerMode.DONT_REQUIRE_LISTENER);
        }
        
        public static bool DirectiveRewardReceivedToday(out string directiveId)
        {
            directiveId = PlayerPrefs.GetString(lastDirectiveRewardId);
            return !String.IsNullOrEmpty(directiveId);
        }

        public static void SaveTakenRewardType(DailyRewardType dailyRewardType)
        {
            if(dailyRewards[1].DailyRewardType == dailyRewardType)
                isLastRewardHard = true;
        }

        /*public static void TakeLastReward()
        {
            if (CountDayBetween == 0)
            {
                Debug.LogError($"count day {CountDayBetween}");
                return;
            }

            var rewards = GetRewards();

            if (CountTakeReward >= rewards.Count)
            {
                return;
            }

            TakeReward(rewards[CountTakeReward].Reward, out WeaponPart directiveReward);
        }*/

        private void AddDays(int count)
        {
            lastDay = day.AddDays(count);
        }

        private void PrintData()
        {
            Debug.Log($"day {day}");
            Debug.Log($"last data {lastDay}");
            Debug.Log($"countDayBetween {CountDayBetween}");
        }

        private void PrintRewards()
        {
            foreach (DailyRewardItem rewardItem in GetRewards())
            {
                Debug.Log($"Status: {rewardItem.StatusType} Reward: {rewardItem.Reward.DailyRewardType}:{rewardItem.Reward.Value}");
            }
        }

        public static StyleBackground GetSprite(DailyRewardType rewardDailyRewardType)
        {
            return rewardDailyRewardType switch
            {
                DailyRewardType.Hard => new StyleBackground(UIHelper.Instance.HardReward),
                DailyRewardType.Soft => new StyleBackground(UIHelper.Instance.SoftCurrencyReward),
                DailyRewardType.Scrap => new StyleBackground(UIHelper.Instance.ScrapReward),
                DailyRewardType.Ticket => new StyleBackground(UIHelper.Instance.TicketReward),
                DailyRewardType.RandomDirective => new StyleBackground(UIHelper.Instance.EmptyDirective),
                _ => new StyleBackground(UIHelper.Instance.SoftCurrencyReward),
            };
        }
    }

    public enum DailyRewardStatusType { ReadyToTake = 2, Taken = 4, NotReady = 1 }

    public enum DailyRewardType { Soft, Hard, Scrap, Ticket, RandomDirective }

    public class DailyRewardItem
    {
        public DailyRewardStatusType StatusType = DailyRewardStatusType.NotReady;
        public DailyReward Reward;
    }

    [Serializable]
    public class DailyReward
    {
        public DailyRewardType DailyRewardType;
        public float Value;

        public int GetRewardAmount
        {
            get
            {
                int missionIndex = DataManager.Instance.GameData.Stars.Count + 1;
                var missionList = DataManager.Instance.Get<MissionList>();

                if (missionList.Missions.Count <= 0)
                    throw new Exception("mission list is empty");

                if (missionIndex >= missionList.Missions.Count)
                    missionIndex = missionList.Missions.Count - 1;

                if (missionIndex < 0)
                    missionIndex = 0;

                if (DailyRewardType == DailyRewardType.Soft)
                    return (int)(missionList.Missions[missionIndex].Reward.SoftCurrency * Value);

                if (DailyRewardType == DailyRewardType.Scrap)
                    return (int)(missionList.Missions[missionIndex].Reward.Scrap * Value);

                return (int)Value;
            }
        }

        public DailyReward(DailyRewardType dailyRewardType, float value)
        {
            DailyRewardType = dailyRewardType;
            Value = value;
        }
    }
}