using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WeighBridgeReader.Classes.WeighBridge
{
    internal class M4222_WBReader : WBReader
    {
        //Length of a default data form a M4222 device
        private readonly int _dataStringLength = 11;
        private readonly int _startData = 2;
        private readonly int _endData = 3;
        private readonly int _lengthOfData = 7;


        public M4222_WBReader(ILogger<Worker> logger, WeighBridgeSettings settings) : base(logger, settings) { }


        /// <summary>
        /// Reads the data given for a M4222 device, matchs the data first to the string of data read and checks for errors
        /// </summary>
        /// <param name="bytesLength">How many bytes of data was read</param>
        /// <param name="bytes">The data read in an array of bytes</param>
        /// <returns>Returns true if data was read and different to last read, false for anything else</returns>
        protected override bool ReadInWeight(int bytesLength, byte[] bytes)
        {
            if (bytesLength < _dataStringLength)
            {
                return false;
            }

            //Start of data check
            int i;
            //End of data check
            int j = _dataStringLength - 1;

            float weight = -1;
            char status = (char)32;

            for (i = 0; j < bytesLength; i++)
            {
                //Check if found a group of sent data
                if (bytes[i] != _startData && bytes[j] != _endData)
                    continue;

                /*
                 * M4222 contacts 11 bytes of data
                 * [0] - Start of the data, alway 0x02 or 2
                 * [1] - Sign on a traffic light linked to the bridge, 0x20 - No sign or light, 0x30 - Red, 0x60 - Green, 0x70 - Red and Green, etc
                 * [2 - 8] - The weight value is sent in bytes 2 to 7, this can be converted to ASCII code and converted to a float, this will lead with spaces
                 * [9] - Status of the bridge, 0x47 - Gross Mode, 0x4E - Net Mode, 0x55 - Under, 0x4F - Over, 0x4D - Motion, 0x45 - Error, 0x20 - ???
                 * [10] - End of transmission, alway 0x03 or 3
                 */

                //Setup a new array to contain the weight data
                byte[] data = new byte[_lengthOfData];
                Array.Copy(bytes, i + 2, data, 0, _lengthOfData);

                //Convert the data read from the string
                bool result = float.TryParse(Encoding.UTF8.GetString(data), out weight);
                //Check if the weight was read correctly
                if (!result)
                    continue;

                //Get the status
                status = (char)bytes[j - 1];

                //Move the reader to the next group of bytes to read
                i = j + 1;
                j += _lengthOfData;
            }

            if (weight == -1)
                return false;

            //Record the last weight found if its different to the recorded weight and status
            if (LastReadValue != weight || WeighBridgeStatus != status)
            {
                LastReadValue = weight;
                WeighBridgeStatus = status;
                return true;
            }

            return false;
        }
    }
}
