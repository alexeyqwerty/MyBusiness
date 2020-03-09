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
    /// <summary>
    /// Логика взаимодействия для MooveGoodsToSale.xaml
    /// </summary>
    public partial class MooveGoodsToSale : Window
    {
        SqlConnection sqlConnect;

        IMoovingFromWarehouseToSale moovingCreator;

        List<MoovingGoodsToSale> moovingList;

        public event RoutedEventHandler GoodsMoved;

        public MooveGoodsToSale(SqlConnection sqlConnect)
        {
            InitializeComponent();

            this.Title += " от " + DateTime.Today.ToShortDateString();

            this.sqlConnect = sqlConnect;

            moovingList = new List<MoovingGoodsToSale>();

            moovingCreator = new MoovingCreator(sqlConnect);

            goodsDescription.sqlConnect = sqlConnect;

            moovingCreator.FillComboWarehouse(comboWarehouseSource);

            moovingCreator.FillComboSeller(comboSeller, true);
            comboSeller.SelectionChanged += ComboSeller_SelectionChanged;

            moovingCreator.FillComboGroups(comboGroups, true);
            comboGroups.SelectionChanged += ComboGroups_SelectionChanged;
        }

        //Изменение выбора склада-поставщика
        private void ComboSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboWarehouseSource.SelectedItem != null && comboGroups.SelectedItem != null)
            {                
                string groupName = comboGroups.SelectedItem.ToString();
                string warehouseSourceName = comboWarehouseSource.SelectedItem.ToString();

                comboGoods.SelectedItem = null;

                moovingCreator.FillComboGoodsInWarehouse(comboGoods, groupName, warehouseSourceName);
            }
        }

        //Изменение выбранного покупателя
        private void ComboSeller_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboSeller.SelectedItem != null)
            {
                if(comboSeller.Items[0].ToString().Equals("Выберите покупателя"))
                {
                    int selectedIndex = comboSeller.SelectedIndex - 1;
                    moovingCreator.FillComboSeller(comboSeller, false);

                    comboSeller.SelectedIndex = selectedIndex;
                }
            }
        }

        //Выбор группы
        private void ComboGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboGroups.SelectedItem != null)
            {
                if (comboGroups.Items[0].Equals("Выберите группу"))
                {
                    int selectedIndex = comboGroups.SelectedIndex - 1;
                    moovingCreator.FillComboGroups(comboGroups, false);

                    comboGroups.SelectedIndex = selectedIndex;
                }

                string groupName = comboGroups.SelectedItem.ToString();
                string warehouseSourceName = comboWarehouseSource.SelectedItem.ToString();

                moovingCreator.FillComboGoodsInWarehouse(comboGoods, groupName, warehouseSourceName);
            }
        }

        //Выбор товара
        private void ComboGoods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboGoods.SelectedItem != null)
            {
                string goodsName = comboGoods.SelectedItem.ToString();

                SelectPercentAge(goodsName);

                goodsDescription.FillAllControlls(goodsName);

                string warehouseSourceName = comboWarehouseSource.SelectedItem.ToString();

                int goodsBalance = moovingCreator.GetGoodsBalanceInWarehouse(goodsName, warehouseSourceName);

                lblGoodsBalance.Content = goodsBalance;

                string measure = moovingCreator.GetGoodsMeasure(goodsName);
                lblMeasure.Content = measure;
                lblMeasureMoove.Content = measure;

                numUpDown.MaxValue = goodsBalance;
                numUpDown.Value = goodsBalance;
            }

            else
            {
                goodsDescription.FillAllControlls(string.Empty);

                lblGoodsBalance.Content = 0;

                lblMeasure.Content = "-";
                lblMeasureMoove.Content = "-";

                numUpDown.MaxValue = 0;
                numUpDown.Value = 0;
            }
        }


        enum ListAction
        {
            AddToList,
            DeleteFromList,
            EditList
        };
        
        private void UpdateGridMooving(ListAction action)
        {
            if(action == ListAction.AddToList)
            {
                for (int i = 0; i < moovingList.Count; i++)
                {
                    bool goodsIdAdded = comboGoods.SelectedItem.ToString().Equals(moovingList[i].GoodsName) ?
                        true : false;
                    bool warehouseSourceIsAdded = comboWarehouseSource.SelectedItem.ToString().Equals(moovingList[i].WarehouseSourse) ?
                        true : false;

                    if (goodsIdAdded && warehouseSourceIsAdded)
                    {
                        string goodsName = moovingList[i].GoodsName;
                        string warehouseSource = moovingList[i].WarehouseSourse;
                        MessageBox.Show(string.Format("Товар {0} от поставщика {1} уже есть в списке. " +
                            "Для изменения количества или цены дважды кликните в соответствующем поле", 
                            goodsName, warehouseSource),
                            "Найдено совпадение", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }
                moovingList.Add(new MoovingGoodsToSale
                {
                    RowNumber = moovingList.Count + 1,
                    GoodsName = comboGoods.SelectedItem.ToString(),
                    GoodsQuantity = numUpDown.Value.ToString(),
                    MeasureUnit = lblMeasure.Content.ToString(),
                    GoodsPrice = tbGoodsPrice.Text,
                    WarehouseSourse = comboWarehouseSource.SelectedItem.ToString()
                });
            }
            
            else if(action == ListAction.DeleteFromList)
            {
                moovingList.RemoveAt(gridMooving.SelectedIndex);
                for (int i = 0; i < moovingList.Count; i++)
                {
                    moovingList[i].RowNumber = i + 1;
                }
            }

            gridMooving.ItemsSource = null;
            gridMooving.ItemsSource = moovingList;
        }

        //Выбор процентной надбавки
        private void radioBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (comboGoods.SelectedItem != null)
                SelectPercentAge(comboGoods.SelectedItem.ToString());
        }

        private void SelectPercentAge(string goodsName)
        {
            if (rbWholesale.IsChecked == true)
                lblReccomendPrice.Content = RecommendPrice(20, goodsName);
            else if (rbRetailsale.IsChecked == true)
                lblReccomendPrice.Content = RecommendPrice(50, goodsName);

            tbGoodsPrice.Text = lblReccomendPrice.Content.ToString();

            lblReccomendPrice.Content += " руб.";
        }

        //Получение рекомендованной цены
        private string RecommendPrice(int percentage, string goodsName)
        {
            string command = "SELECT [Goods].[PurchasePrice] FROM [Goods] WHERE [GoodsName] = '" + goodsName + "'";

            decimal purchasePrice = decimal.Parse(new DataBaseReader(this.sqlConnect).GetString(command));
            decimal valuePercentage = (purchasePrice * percentage) / 100;
            decimal priceWithPercentage = purchasePrice + valuePercentage;

            return string.Format("{0:#,#.00}", priceWithPercentage);
        }


        private void BtnGoodsAdd_Click(object sender, RoutedEventArgs e)
        {
            int goodsQuantity = numUpDown.Value;
            bool goodsIsSelected = comboGoods.SelectedItem != null ? true : false;

            if (goodsIsSelected && (goodsQuantity > 0))
            {
                UpdateGridMooving(ListAction.AddToList);
            }

            else if (!goodsIsSelected)
            {
                ErrorMessages.GoodsNotSelected();
            }

            else if (goodsQuantity == 0)
            {
                ErrorMessages.ZeroValue();
            }
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if(gridMooving.SelectedItem != null)
            {
                UpdateGridMooving(ListAction.DeleteFromList);
            }
        }

        private List<Warehouses> GetWarehousesList()
        {
            List<Warehouses> listWarehouses = new List<Warehouses>();
            DataBaseReader dbReader = new DataBaseReader(this.sqlConnect);

            string command = "SELECT [Warehouses].[WarehouseID], " +
                "[Warehouses].[WarehouseName] FROM [Warehouses]";

            dbReader.GetSqlReader(command);

            while(dbReader.SqlReader.Read())
            {
                listWarehouses.Add(new Warehouses
                {
                    RowNumber = dbReader.SqlReader[0].ToString(),
                    WarehouseName = dbReader.SqlReader[1].ToString()
                });
            }

            dbReader.CloseReader();

            return listWarehouses;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            bool sellerIsSelected = 
                (comboSeller.SelectedItem != null &&
                !comboSeller.SelectedItem.ToString().Equals("Выберите покупателя")) ? true : false;

            bool tableIsFiled = moovingList.Count > 0 ? true : false;

            if(!sellerIsSelected)
            {
                MessageBox.Show("Выберите продавца из списка или создайте нового",
                    "Продавец не выбран", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            else if(!tableIsFiled)
            {
                ErrorMessages.MoovingTableIsEmpty();
            }

            else if(sellerIsSelected && tableIsFiled)
            {
                DataBaseReader dbReader = new DataBaseReader(this.sqlConnect);

                string sellerID;
                string sellerName = comboSeller.SelectedItem.ToString();

                string command = "SELECT [Buyers].[BuyerID] FROM [Buyers] " +
                    "WHERE [Buyers].[BuyerName] = '" + sellerName + "'";

                sellerID = dbReader.GetString(command);

                string lastUpdate = Convert.ToDateTime(DateTime.Now).ToString("yyyyMMdd");

                List<Warehouses> listWarehouses = GetWarehousesList();

                for (int i = 0; i < moovingList.Count; i++)
                {
                    string goodsID;
                    string goodsName = moovingList[i].GoodsName;

                    command = "SELECT [Goods].[GoodsID] FROM [Goods] " +
                        "WHERE [Goods].[GoodsName] = '" + goodsName + "'";

                    goodsID = dbReader.GetString(command);                    

                    string goodsQuantity = moovingList[i].GoodsQuantity;
                    string goodsPrice = moovingList[i].GoodsPrice;

                    //ID склада
                    string warehouseSourse = moovingList[i].WarehouseSourse;
                    int index = listWarehouses.FindIndex(
                        indx => string.Equals(indx.WarehouseName, warehouseSourse, StringComparison.CurrentCultureIgnoreCase));
                    string warehouseSourceID = listWarehouses[index].RowNumber;

                    command = "SELECT [GoodsInSale].[GoodsName] FROM [GoodsInSale] " +
                        "WHERE [GoodsInSale].[BuyerName] = '" + sellerID + "' " +
                        "AND [GoodsInSale].[GoodsName] = '" + goodsID + "'";
                    bool rowIsFinded = dbReader.SearchMatch(command);

                    DataBaseWriter dbWriter = new DataBaseWriter(this.sqlConnect);

                    if(rowIsFinded)
                    {
                        command = "SELECT [GoodsInSale].[GoodsQuantity] FROM [GoodsInSale] " +
                            "WHERE [GoodsInSale].[BuyerName] = '" + sellerID + "' " +
                            "AND [GoodsInSale].[GoodsName] = '" + goodsID + "'";
                        float oldQuantityFromSeller = float.Parse(dbReader.GetString(command));
                        float newQuantityFromSeller = oldQuantityFromSeller + float.Parse(goodsQuantity);

                        command = "UPDATE [GoodsInSale] " +
                            "SET [GoodsQuantity] = '" + TextHandler.FloatToString(newQuantityFromSeller) + "', " +
                            "[GoodsPrice] = '" + goodsPrice + "', " +
                            "[LastUpdate] = '" + lastUpdate + "' " +
                            "WHERE [GoodsInSale].[BuyerName] = '" + sellerID + "' " +
                            "AND [GoodsInSale].[GoodsName] = '" + goodsID + "'";
                        dbWriter.WriteData(command);
                    }
                    else
                    {
                        //Продавец
                        dbWriter.InsDataRow.Add(new InsertDataToRow { ColumnName = "BuyerName", Data = sellerID });

                        //Товар
                        dbWriter.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsName", Data = goodsID });

                        //Количество
                        dbWriter.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsQuantity", Data = goodsQuantity });

                        //Цена
                        dbWriter.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsPrice", Data = goodsPrice });

                        //Дата последней поставки
                        dbWriter.InsDataRow.Add(new InsertDataToRow { ColumnName = "LastUpdate", Data = lastUpdate });

                        command = "INSERT INTO [GoodsInSale] (BuyerName, GoodsName, GoodsQuantity, GoodsPrice, LastUpdate) " +
                            "VALUES (@BuyerName, @GoodsName, @GoodsQuantity, @GoodsPrice, @LastUpdate)";

                        dbWriter.WriteData(command);                        
                    }

                    //Обновления количества товара на складе поставщике
                    command = "SELECT [GoodsInWarehouses].[GoodsBalance] FROM [GoodsInWarehouses] " +
                        "WHERE [GoodsInWarehouses].[WarehouseName] = '" + warehouseSourceID + "' " +
                        "AND [GoodsInWarehouses].[GoodsName] = '" + goodsID + "'";

                    float oldQuantity = float.Parse(dbReader.GetString(command));
                    float newQuantity = oldQuantity - float.Parse(goodsQuantity);

                    DataBaseWriter updateData = new DataBaseWriter(this.sqlConnect);
                    command = "UPDATE [GoodsInWarehouses]" +
                        "SET [GoodsBalance] = " + TextHandler.FloatToString(newQuantity) + " " +
                        "WHERE [GoodsName] = '" + goodsID + "'" +
                        "AND [WarehouseName] = '" + warehouseSourceID + "'";
                    updateData.WriteData(command);
                }

                GoodsMoved?.Invoke(sender, e);

                MessageBox.Show(string.Format("Перемещение {0} продавцу {1} успешно завершено",
                    moovingList.Count > 1 ? "товаров" : "товара", sellerName), "Выполнено",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }      

        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        //Фильтрация ввода
        //----------------

        private void GridMooving_BeginingEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if(e.Column.DisplayIndex != 2 && e.Column.DisplayIndex != 4)
            {
                e.Cancel = true;
            }
        }

        private void GridMooving_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                TextBox tb = e.EditingElement as TextBox;

                TextHandler.DotReplace(tb);

                float temp = float.Parse(tb.Text);

                int index = gridMooving.SelectedIndex;

                switch (e.Column.SortMemberPath)
                {
                    case "GoodsQuantity":
                        {
                            MoovingGoodsToSale gridRow = gridMooving.SelectedItem as MoovingGoodsToSale;

                            string goodsName = gridRow.GoodsName;
                            string warehouseName = gridRow.WarehouseSourse;

                            string command = "SELECT [GoodsInWarehouses].[GoodsBalance] FROM [GoodsInWarehouses] " +
                                "INNER JOIN [Warehouses] " +
                                "ON [GoodsInWarehouses].[WarehouseName] = [Warehouses].[WarehouseID] " +
                                "INNER JOIN [Goods] " +
                                "ON [GoodsInWarehouses].[GoodsName] = [Goods].[GoodsID] " +
                                "WHERE [Goods].[GoodsName] = '" + goodsName + "' " +
                                "AND [Warehouses].[WarehouseName] = '" + warehouseName + "'";

                            float goodsQuantity = float.Parse(new DataBaseReader(this.sqlConnect).GetString(command));

                            moovingList[index].GoodsQuantity =
                                goodsQuantity >= temp ? temp.ToString() : goodsQuantity.ToString();
                        }
                        break;
                    case "GoodsPrice":
                        moovingList[index].GoodsPrice = StringTransformer.TransformDecimal(temp.ToString());
                        break;
                }                
            }

            catch
            {
                ErrorMessages.UncorrectData();
            }

            finally
            {
                UpdateGridMooving(ListAction.EditList);
            }
        }

        private void TbGoodsPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextHandler.DotReplace(tbGoodsPrice);
        }

        private void PrewiewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string text = textBox != null ? textBox.Text : string.Empty;
            e.Handled = TextHandler.Filter(e.Text, text);
        }

        private void TextPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        //---------------

    }
}
