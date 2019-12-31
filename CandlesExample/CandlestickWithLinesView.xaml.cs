using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SciChart.Charting.Model.DataSeries;
using System.ComponentModel;
using System.Dynamic;
using SciChart.Charting.Visuals.Annotations;
using System.Windows.Media;

namespace CandlesExample {
    public partial class CandlestickWithLinesView : UserControl {


        public CandlestickWithLinesView() {
            InitializeComponent();
        }

        private void CandlesticksWithLines_Loaded(object sender, RoutedEventArgs e) {
            var dataSeries0 = new OhlcDataSeries<DateTime, double>();
            var dataSeries1 = new XyDataSeries<DateTime, long>();

            var viewModel = new CandlesViewModel();
            var prices = viewModel.Prices;
            dataSeries0.Append(
                prices.TimeData,
                prices.OpenData,
                prices.HighData,
                prices.LowData,
                prices.CloseData
                );
            dataSeries1.Append(prices.TimeData, prices.VolumeData);
            sciChart.RenderableSeries[0].DataSeries = dataSeries0;
            sciChart.RenderableSeries[1].DataSeries = dataSeries1;
            int index = 0;
            foreach (var pattern in prices.Patterns) {
                if (pattern > 0)
                    sciChart.Annotations.Add(new TextAnnotation() { Text = pattern.ToString(), X1 = prices.TimeData[index], Y1 = prices.OpenData[index], Background = Brushes.Blue });
                index++;
            }

            sciChart.ZoomExtents();
        }
    }

    public class CandlesViewModel : DependencyObject, IDisposable {
        public static readonly DependencyProperty PricesProperty =
            DependencyProperty.Register("Prices", typeof(PriceData), typeof(CandlesViewModel), new PropertyMetadata(null));

        public CandlesViewModel() {
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            Prices = new PriceData();
            using (var reader = new StreamReader(@"MSFT_2500lines.csv")) {
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    var date = DateTime.Parse(values[0]);
                    var open = ParseDouble(values[1]);
                    var high = ParseDouble(values[2]);
                    var low = ParseDouble(values[3]);
                    var close = ParseDouble(values[4]);
                    var volume = long.Parse(values[5]);
                    Prices.Add(new PriceDataItem(date, open, high, low, close, volume));
                }
            }
            Prices.CalcPatterns();
        }

        double ParseDouble(string s) {
            return double.Parse(s, CultureInfo.InvariantCulture);
        }

        public void Dispose() {
            PythonEngine.Shutdown();
        }

        public PriceData Prices {
            get { return (PriceData)GetValue(PricesProperty); }
            set { SetValue(PricesProperty, value); }
        }
    }

    public class PriceData : List<PriceDataItem> {

        public PriceData() {
        }

        public PriceData(int capacity) : base(capacity) {
        }

        IEnumerable<float> CalcPatternsCore(string methodName) {
            using (Py.GIL()) {
                dynamic np = Py.Import("numpy");
                dynamic ta = Py.Import("talib._ta_lib");

                dynamic npopen = np.array(OpenData.Select(x => (float)x).ToList());
                dynamic npclose = np.array(CloseData.Select(x => (float)x).ToList());
                dynamic nphigh = np.array(HighData.Select(x => (float)x).ToList());
                dynamic nplow = np.array(LowData.Select(x => (float)x).ToList());

                IEnumerable<float> result = ta.CDL3OUTSIDE(npopen, nphigh, nplow, npclose).AsManagedObject(typeof(float[]));
                var val = result.FirstOrDefault(x => x != 0);
                return result;
            }
        }
        public IEnumerable<float> Patterns { get { return patterns; } }
        IEnumerable<float> patterns = null;
        public void CalcPatterns() {
            patterns = CalcPatternsCore("");
        }

        public PriceData Clip(int startIndex, int endIndex) {
            var result = new PriceData(endIndex - startIndex);
            for (int i = startIndex; i < endIndex; i++) {
                result.Add(this[i]);
            }
            return result;
        }

        public IList<double> CloseData { get { return this.Select(x => x.Close).ToArray(); } }

        public IList<double> HighData { get { return this.Select(x => x.High).ToArray(); } }

        public IList<double> LowData { get { return this.Select(x => x.Low).ToArray(); } }

        public IList<double> OpenData { get { return this.Select(x => x.Open).ToArray(); } }
        public string Symbol { get; set; }

        public IList<DateTime> TimeData { get { return this.Select(x => x.DateTime).ToArray(); } }

        public IList<long> VolumeData {
            get {
                var max = this.Max(x => x.Volume);
                CalcPatterns();
                return this.Select(x => (long)((double)x.Volume / max * 100)).ToArray();
            }
        }
    }

    public class PriceDataItem {
        public PriceDataItem() {
        }

        public PriceDataItem(DateTime date, double open, double high, double low, double close, long volume) {
            DateTime = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        public double Close { get; set; }

        public DateTime DateTime { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Open { get; set; }
        public long Volume { get; set; }
    }
}
