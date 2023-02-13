using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeighBridgeReader.Classes
{
    internal class WeighbridgePOSTData
    {
        private enum DataverseStatus { Unknown = 198320000, Gross, Net, Under, Over, Motion, Error}


        private string _weighbridgeGUID = string.Empty;

        private string _name = string.Empty;
        private string _status = string.Empty;

        public string Weighbridge { get => _name; set => _name = value; }
        public float Weight { get; set; }
        public string Status { get => _status; set=> _status = value; }
        public DateTime Time { get; set; }


        public string WeighbridgeGUID { get => _weighbridgeGUID; set => _weighbridgeGUID = value; }


        /// <summary>
        /// Converts the current object and into a Dataverse JSON object for a row of data
        /// </summary>
        /// <param name="enviromentName">Name of the enviroments table name section</param>
        /// <param name="weighbridgeTableName">Name of the weighbridge table, the full name</param>
        /// <returns>String of the JSON object</returns>
        public string JsonSerialize(string enviromentName, string weighbridgeTableName)
        {
            Dictionary<string, object> jsonData = new Dictionary<string, object>();
            jsonData.Add($"{enviromentName}_Weighbridge@odata.bind", $"{enviromentName}_{weighbridgeTableName}({WeighbridgeGUID})");
            jsonData.Add($"{enviromentName}_weighbridgestatus", DataverseStatusValue());
            jsonData.Add($"{enviromentName}_enteredtime", Time.ToString("yyyy-MM-ddTHH:mm:ssK"));
            jsonData.Add($"{enviromentName}_weight", Weight);

            return JsonSerializer.Serialize(jsonData);
        }


        /// <summary>
        /// Return the value of the Dataverse Status
        /// </summary>
        /// <returns>value of the status used for dataverse</returns>
        private int DataverseStatusValue()
        {
            switch(_status)
            {
                case "G":
                    return ((int)DataverseStatus.Gross);
                case "N":
                    return ((int)DataverseStatus.Net);
                case "U":
                    return ((int)DataverseStatus.Under);
                case "O":
                    return ((int)DataverseStatus.Over);
                case "M":
                    return ((int)DataverseStatus.Motion);
                case "E":
                    return ((int)DataverseStatus.Error);
            }

            return ((int)DataverseStatus.Unknown);
        }
    }
}
