﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Shiny.Locations
{
    public static class MotionActivityExtensions
    {
        /// <summary>
        /// Queries for the most current event
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="maxAge">Timespan containing max age of "current"</param>
        /// <returns></returns>
        public static async Task<MotionActivityEvent> GetCurrentActivity(this IMotionActivity activity, TimeSpan? maxAge = null)
        {
            maxAge = maxAge ?? TimeSpan.FromMinutes(5);
            var end = DateTimeOffset.UtcNow;
            var result = (await activity.Query(end.Subtract(maxAge.Value), end)).OrderBy(x => x.Timestamp).FirstOrDefault();
            return result;
        }


        public static async Task<IList<MotionActivityTimeBlock>> GetTimeBlocksForRange(this IMotionActivity activity,
                                                                                       DateTimeOffset start,
                                                                                       DateTimeOffset? end = null,
                                                                                       MotionActivityConfidence minConfidence = MotionActivityConfidence.Medium)
        {
            var list = new List<MotionActivityTimeBlock>();
            var result = await activity.Query(start, end);
            var set = result
                .Where(x => x.Confidence >= minConfidence)
                .OrderBy(x => x.Timestamp)
                .ToList();

            if (set.Count > 1)
            {
                MotionActivityEvent? firstEvent = null;
                foreach (var item in set)
                {
                    if (firstEvent == null)
                        firstEvent = item;
                    else if (!firstEvent.Value.Types.HasFlag(item.Types)) // has to have 1 of the types
                    {
                        var block = new MotionActivityTimeBlock(item.Types, firstEvent.Value.Timestamp, item.Timestamp);
                        list.Add(block);

                        // first event of next time block
                        firstEvent = item;
                    }

                }
            }

            return list;
        }


        public static async Task<IDictionary<MotionActivityType, TimeSpan>> GetTotalsForRange(this IMotionActivity activity,
                                                                                              DateTimeOffset start,
                                                                                              DateTimeOffset? end = null,
                                                                                              MotionActivityConfidence minConfidence = MotionActivityConfidence.Medium)
        {
            var dict = new Dictionary<MotionActivityType, TimeSpan>();
            var result = await activity.Query(start, end);
            var set = result
                .Where(x => x.Confidence >= minConfidence)
                .OrderBy(x => x.Timestamp)
                .ToList();

            if (set.Count > 1)
            {
                MotionActivityEvent? lastEvent = null;
                foreach (var item in set)
                {
                    if (lastEvent == null)
                        lastEvent = item;
                    else
                    {
                        if (!dict.ContainsKey(item.Types))
                            dict.Add(item.Types, TimeSpan.Zero);

                        var ts = item.Timestamp.Subtract(lastEvent.Value.Timestamp);
                        dict[item.Types] += ts;
                    }

                }
            }
            return dict;
        }


        /// <summary>
        /// Queries for the most current event and checks against type & confidence
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="type"></param>
        /// <param name="maxAge"></param>
        /// <param name="minConfidence"></param>
        /// <returns></returns>
        public static async Task<bool> IsCurrentActivity(this IMotionActivity activity, MotionActivityType type, TimeSpan? maxAge = null, MotionActivityConfidence minConfidence = MotionActivityConfidence.Medium)
        {
            var result = await activity.GetCurrentActivity(maxAge);
            //if (result == default(MotionActivityEvent))
                //return false;
            if (result.Confidence < minConfidence)
                return false;

            return result.Types.HasFlag(type);
        }


        /// <summary>
        /// Queries if most recent activity is automotive
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="maxAge"></param>
        /// <param name="minConfidence"></param>
        /// <returns></returns>
        public static Task<bool> IsCurrentAutomotive(this IMotionActivity activity, TimeSpan? maxAge = null, MotionActivityConfidence minConfidence = MotionActivityConfidence.Medium)
            => activity.IsCurrentActivity(MotionActivityType.Automotive, maxAge, minConfidence);


        /// <summary>
        /// Queries if most recent activity is stationary
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="maxAge"></param>
        /// <param name="minConfidence"></param>
        /// <returns></returns>
        public static Task<bool> IsCurrentStationary(this IMotionActivity activity, TimeSpan? maxAge = null, MotionActivityConfidence minConfidence = MotionActivityConfidence.Medium)
            => activity.IsCurrentActivity(MotionActivityType.Stationary, maxAge, minConfidence);


        /// <summary>
        /// Queries for activities for an entire day (beginning to end)
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Task<IList<MotionActivityEvent>> QueryByDate(this IMotionActivity activity, DateTimeOffset date)
            => activity.Query(date.Date, new DateTimeOffset(date.Date.AddDays(1)));



    }
}
