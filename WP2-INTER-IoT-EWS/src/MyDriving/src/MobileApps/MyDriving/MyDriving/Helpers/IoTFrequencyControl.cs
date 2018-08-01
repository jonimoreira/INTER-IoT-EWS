using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDriving.Helpers
{
    public class IoTFrequencyControl
    {
        private double frequencyIoTDeviceToGatewayInHertz = 256.0;
        private double periodGatewayToCloudInSeconds = 3.0;
        private int counterSamplesInMessage = 0;

        public double FrequencyIoTDeviceToGatewayInHertz
        {
            get
            {
                return frequencyIoTDeviceToGatewayInHertz;
            }

            set
            {
                frequencyIoTDeviceToGatewayInHertz = value;
            }
        }

        public double PeriodGatewayToCloudInSeconds
        {
            get
            {
                return periodGatewayToCloudInSeconds;
            }

            set
            {
                periodGatewayToCloudInSeconds = value;
            }
        }

        public int CounterSamplesInMessage
        {
            get
            {
                return counterSamplesInMessage;
            }

            set
            {
                counterSamplesInMessage = value;
            }
        }

        public IoTFrequencyControl()
        { }
    }
}
