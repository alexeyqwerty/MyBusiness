using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyBusiness
{
    public class PriceListRules
    {
        private string fontFamily;
        private double fontSize;
        private bool allGroups;
        private bool addPicture;
        private string percentage;

        public PriceListRules()
        {
            allGroups = true;
            addPicture = true;

            percentage = "0.2";
        }

        public string FontFamily { get => fontFamily; set => fontFamily = value; }
        public double FontSize { get => fontSize; set => fontSize = value; }
        public bool AllGroups { get => allGroups; set => allGroups = value; }
        public bool AddPicture { get => addPicture; set => addPicture = value; }
        public string Percentage { get => percentage; set => percentage = value; }

    }

    public class GridGroups
    {
        int rowNumber;
        string group;

        public int RowNumber { get => rowNumber; set => rowNumber = value; }
        public string Group { get => group; set => group = value; }
    }

    public partial class CreatePriceWindow : Window
    {
        SqlConnection sqlConnect;

        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, Object value);
        UpdateProgressBarDelegate updatePbDelegate;

        public PriceListRules rules { get; }
        public List<GridGroups> listGroups { get; }

        public CreatePriceWindow(SqlConnection sqlConnect)
        {
            InitializeComponent();

            this.sqlConnect = sqlConnect;

            rules = new PriceListRules();            

            //Заполнение комбо группы
            IComboGoodsGroupsFiller groupsFiller = new ComboBoxFiller(this.sqlConnect);
            groupsFiller.FillComboGoodsGroups(comboGroups, false, false);

            listGroups = new List<GridGroups>();

            updatePbDelegate = new UpdateProgressBarDelegate(prgBar.SetValue);

            checkAddPic.Checked += CheckBox_Checked;
            checkAddPic.Unchecked += CheckBox_Unchecked;

            FillComboFontName();
            FillComboFontSize();
        }

        //Заполнение комбо шрифты
        private void FillComboFontName()
        {
            List<string> fonts = new List<string>();

            foreach(FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                fonts.Add(fontFamily.ToString());
            }

            cbFontName.ItemsSource = fonts;
            cbFontName.SelectionChanged += CbFontName_SelectionChanged;
            cbFontName.SelectedIndex = 0;
        }

        //Выбор шрифта
        private void CbFontName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FontFamily fontFamily = new FontFamily(cbFontName.SelectedItem.ToString());

            tbExample.Text = fontFamily.ToString();
            tbExample.FontFamily = fontFamily;

            rules.FontFamily = fontFamily.ToString();
        }

        private void FillComboFontSize()
        {
            List<double> fontSizes = new List<double>() {8, 9, 10, 12, 13, 14, 16, 18};

            cbFontSize.ItemsSource = fontSizes;
            cbFontSize.SelectionChanged += CbFontSize_SelectionChanged;
            cbFontSize.SelectedIndex = 0;
        }

        private void CbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            double fontSize = double.Parse(cbFontSize.SelectedItem.ToString());

            tbExample.FontSize = fontSize;

            rules.FontSize = fontSize;
        }

        //Обновление прогресс бара
        public void UpdateProgressBar(double progress)
        {            
            progress += prgBar.Value;

            double persent = (progress / prgBar.Maximum) * 100;
            lblPersent.Content = StringTransformer.TransformDecimal(persent.ToString()) + "%";

            Dispatcher.Invoke(updatePbDelegate, System.Windows.Threading.DispatcherPriority.Background,
            new object[] { ProgressBar.ValueProperty, progress });
        }
        
        //Установка максимального значения для прогресс бара
        private void SetPrgBarMax()
        {
            string command = string.Empty;

            if (rules.AllGroups)
            {
                command =
                "WITH " +

                //Остаток на складах
                "SumInWarehouses(GoodsID, SumInWarehouses) AS " +
                "(SELECT [Goods].[GoodsID] AS GoodsID, " +
                "SUM([GoodsInWarehouses].[GoodsBalance]) AS SumInWarehouses " +
                "FROM [GoodsInWarehouses] " +
                "INNER JOIN [Goods] " +
                "ON [GoodsInWarehouses].[GoodsName] = [Goods].[GoodsID] " +
                "GROUP BY [Goods].[GoodsID]), " +

                //Остаток у реализаторов
                "SumInSale(GoodsID, SumInSale) AS " +
                "(SELECT [Goods].[GoodsID] AS GoodsID, " +
                "SUM([GoodsInSale].[GoodsQuantity]) AS SumInSale " +
                "FROM [GoodsInSale] " +
                "INNER JOIN [Goods] " +
                "ON [GoodsInSale].[GoodsName] = [Goods].[GoodsID] " +
                "GROUP BY [Goods].[GoodsID])" +

                "SELECT COUNT([Goods].[GoodsID]) " +
                "FROM [Goods] " +
                "FULL OUTER JOIN SumInWarehouses " +
                "ON SumInWarehouses.GoodsID = [Goods].[GoodsID] " +
                "FULL OUTER JOIN SumInSale " +
                "ON SumInSale.GoodsID = [Goods].[GoodsID] " +
                "WHERE (SumInWarehouses.SumInWarehouses + ISNULL(SumInSale.SumInSale, 0)) > 0 ";
            }

            else
            {
                for (int groupNum = 0; groupNum < listGroups.Count; groupNum++)
                {
                    command =
                    "WITH " +

                    //Остаток на складах
                    "SumInWarehouses(GoodsID, SumInWarehouses) AS " +
                    "(SELECT [Goods].[GoodsID] AS GoodsID, " +
                    "SUM([GoodsInWarehouses].[GoodsBalance]) AS SumInWarehouses " +
                    "FROM [GoodsInWarehouses] " +
                    "INNER JOIN [Goods] " +
                    "ON [GoodsInWarehouses].[GoodsName] = [Goods].[GoodsID] " +
                    "GROUP BY [Goods].[GoodsID]), " +

                    //Остаток у реализаторов
                    "SumInSale(GoodsID, SumInSale) AS " +
                    "(SELECT [Goods].[GoodsID] AS GoodsID, " +
                    "SUM([GoodsInSale].[GoodsQuantity]) AS SumInSale " +
                    "FROM [GoodsInSale] " +
                    "INNER JOIN [Goods] " +
                    "ON [GoodsInSale].[GoodsName] = [Goods].[GoodsID] " +
                    "GROUP BY [Goods].[GoodsID])" +

                    "SELECT COUNT([Goods].[GoodsID]) " +
                    "FROM [Goods] " +
                    "INNER JOIN [GoodsGroups] " +
                    "ON [GoodsGroups].[GroupID] = [Goods].[GoodsGroup] " +
                    "FULL OUTER JOIN SumInWarehouses " +
                    "ON SumInWarehouses.GoodsID = [Goods].[GoodsID] " +
                    "FULL OUTER JOIN SumInSale " +
                    "ON SumInSale.GoodsID = [Goods].[GoodsID] " +
                    "WHERE [GoodsGroups].[GroupName] = '" + listGroups[groupNum].Group + "' " +
                    "AND (SumInWarehouses.SumInWarehouses + ISNULL(SumInSale.SumInSale, 0)) > 0 ";
                }
            }

            int goodsCount = int.Parse(new DataBaseReader(sqlConnect).GetString(command));

            prgBar.Maximum = goodsCount;
        }

        public void StartVisualiseCreationProcess()
        {
            SetPrgBarMax();

            gridMenu.Visibility = Visibility.Hidden;
            gridWait.Visibility = Visibility.Visible;
        }

        //Выбор между всеми группами и некоторыми
        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            if (bdrSelectGroups != null)
            {
                RadioButton radio = sender as RadioButton;

                if (radio == radioAllGroups)
                {
                    bdrSelectGroups.IsEnabled = false;
                    rules.AllGroups = true;
                }
                else if (radio == radioSelectedGroups)
                {
                    bdrSelectGroups.IsEnabled = true;
                    rules.AllGroups = false;
                }
            }
        }

        //Добавление товара в таблицу
        private void BtnAddGroup_Click(object sender, RoutedEventArgs e)
        {
            if(comboGroups.SelectedItem != null)
            {
                string group = comboGroups.SelectedItem.ToString();

                listGroups.Add(new GridGroups
                {
                    RowNumber = listGroups.Count + 1,
                    Group = group
                });

                gridGroups.ItemsSource = null;
                gridGroups.ItemsSource = listGroups;
            }
        }

        //Удаление товара из таблицы
        private void BtnDeleteGroupRow_Click(object sender, RoutedEventArgs e)
        {
            if(gridGroups.SelectedItem != null)
            {
                int index = gridGroups.SelectedIndex;

                listGroups.RemoveAt(index);

                for (int i = 0; i < listGroups.Count; i++)
                {
                    listGroups[i].RowNumber = i + 1;
                }

                gridGroups.ItemsSource = null;
                gridGroups.ItemsSource = listGroups;
            }
        }
        
        //С картинкой
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            rules.AddPicture = true;
        }

        //Без картинки
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            rules.AddPicture = false;
        }

        //Изменение процентной надбавки
        private void Slide_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            double percentage = int.Parse(lblPercentage.Content.ToString()) * 0.01;

            rules.Percentage = percentage.ToString().Replace(',', '.');
        }

        //Создание прайса
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if(!rules.AllGroups && listGroups.Count == 0)
            {
                MessageBox.Show("Выберите группу(ы) для создания прайс-листа",
                    "Группы не выбраны", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            IGoodsPriceReport priceReport = new DocumentCreator(sqlConnect);
            if (priceReport.CreateGoodsPriceReport(this))
            {
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
