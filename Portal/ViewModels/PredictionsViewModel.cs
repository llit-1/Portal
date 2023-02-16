using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels
{    
    public class PredictionsViewModel
    {
        public List<RKNet_Model.TT.TT> TTs;
        public List<Models.MSSQL.SalesPrediction> Predictions;
        public int MonthCount;

        // наиболее часто встречающееся значение прогноза (отображается в группировке)
        public int MaxCountGroupValue(DateTime date, string type)
        {
            var typeTTs = TTs.Where(t => t.Type.Name == type);
            var typePredictions = new List<Models.MSSQL.SalesPrediction>();
            var dateString = date.ToString("yyyy.MM.dd");


            foreach(var t in typeTTs)
            {
                var data = Predictions.Where(p => p.TTCode == t.Code & p.Date == dateString);
                if (data.Count() > 0)
                {
                    typePredictions.Add(data.First());
                }
            }

            var counts = new Dictionary<int, int>();

            foreach(var p in typePredictions)
            {
                if(counts.ContainsKey(p.PredictionValue))
                {
                    counts[p.PredictionValue] += 1; 
                }
                else
                {
                    counts.Add(p.PredictionValue, 1);
                }
            }

            var maxCount = counts.OrderByDescending(c => c.Value).FirstOrDefault();

            return maxCount.Key;
        }

        // среднее значение в группировке
        public int AverageGroupValue(DateTime date, string type)
        {
            var typeTTs = TTs.Where(t => t.Type.Name == type);
            var typePredictions = new List<Models.MSSQL.SalesPrediction>();
            var dateString = date.ToString("yyyy.MM.dd");


            foreach (var t in typeTTs)
            {
                var data = Predictions.Where(p => p.TTCode == t.Code & p.Date == dateString);
                if (data.Count() > 0)
                {
                    typePredictions.Add(data.First());
                }
            }

            decimal sum = 0;

            foreach (var p in typePredictions)
            {
                sum += p.PredictionValue;
            }

            if (typePredictions.Count() > 0)
            {
                var average = Math.Round(sum / typePredictions.Count());
                return Convert.ToInt32(average);
            }
            else
            {
                return 0;
            }
            
        }
    }
}
