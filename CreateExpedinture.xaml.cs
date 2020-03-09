using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyBusiness
{
    /// <summary>
    /// Логика взаимодействия для CreateExpedinture.xaml
    /// </summary>
    /// 
    class UnworkInfo
    {
        int invoiceID;

        public UnworkInfo(int invoiceID)
        {
            this.InvoiceID = invoiceID;
        }

        public int InvoiceID { get => invoiceID; private set => invoiceID = value; }
    }

    public partial class CreateExpedinture : Window
    {
        SqlConnection sqlConnect;

        IExpedintureCreator expedintureCreator;

        List<ExpedintureInvoiceContent> invoiceList;

        public event RoutedEventHandler BuyerAdded;

        TabItem tabItem;

        UnworkInfo unworkInfo;

        private void startWindow(SqlConnection sqlConnect, bool withSelectBuyer)
        {
            InitializeComponent();

            this.sqlConnect = sqlConnect;

            expedintureCreator = new InvoiceCreator(sqlConnect);

            goodsDescription.sqlConnect = sqlConnect;
            buyerAdd.sqlConnect = sqlConnect;

            invoiceList = new List<ExpedintureInvoiceContent>();
            
            FillAllControlls(withSelectBuyer);

            comboGroups.SelectionChanged += ComboGroups_SelectionChanged;

            comboBuyers.SelectionChanged += ComboBuyers_SelectionChanged;

            buyerAdd.BuyerAdded += (o, eve) => _BuyerAdded(o, eve);

            tabItem = tabSale.Items[0] as TabItem;
        }

        public CreateExpedinture(SqlConnection sqlConnect)
        {
            startWindow(sqlConnect, true);
        }

        public CreateExpedinture(SqlConnection sqlConnect, int unworkInvoceId)
        {
            startWindow(sqlConnect, false);

            DataBaseReader reader = new DataBaseReader(sqlConnect);

            string command = "SELECT [Buyers].[BuyerName] FROM [Buyers] " +
                "INNER JOIN [ExpedintureUnwork] " +
                "ON [ExpedintureUnwork].[BuyerName] = [Buyers].[BuyerID] " +
                "WHERE [ExpedintureUnwork].[InvoiceID] = '" + unworkInvoceId + "'";
            string buyerName = reader.GetString(command);

            command = "SELECT [Warehouses].[WarehouseName] FROM [Warehouses] " +
                "INNER JOIN [ExpedintureUnwork] " +
                "ON [ExpedintureUnwork].[WarehouseName] = [Warehouses].[WarehouseID] " +
                "WHERE [ExpedintureUnwork].[InvoiceID] = '" + unworkInvoceId + "'";

            string warehouseName = reader.GetString(command);

            comboBuyers.SelectionChanged -= new SelectionChangedEventHandler(ComboBuyers_SelectionChanged);
            comboBuyers.SelectedItem = buyerName;
            lblSellerName.Content = buyerName;
            comboBuyers.SelectionChanged += new SelectionChangedEventHandler(ComboBuyers_SelectionChanged);            

            IExpedintureUnwork unwork = new InvoiceCreator(this.sqlConnect);
            invoiceList = unwork.GetInvoiceListFromUnworkData(unworkInvoceId);

            FillGridExpedinture(ListAction.EditList);

            unworkInfo = new UnworkInfo(unworkInvoceId);

            FillInfoControlsBuyerGoodsInSale(buyerName);

            foreach(ExpedintureInvoiceContent content in invoiceList)
            {
                if(content.GoodsSource.Equals(buyerName))
                {
                    comboBuyers.IsEnabled = false;
                    break;
                }
            }

            foreach (ExpedintureInvoiceContent content in invoiceList)
            {
                if (content.GoodsSource.Equals(warehouseName))
                {
                    comboWarehouse.IsEnabled = false;
                    break;
                }
            }
        }

        //Изменение выбранного табконтрола источника товаров
        private void tabSale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.OriginalSource is TabControl)
            {
                tabItem = tabSale.SelectedItem as TabItem;

                string goodsName = tabItem.Name == "itemSaleFromWarehouse" ?
                    (comboGoods.SelectedItem != null ? comboGoods.SelectedItem.ToString() : string.Empty) :
                    (comboBuyerGoodsInSale.SelectedItem != null ? comboBuyerGoodsInSale.SelectedItem.ToString() : string.Empty);

                goodsDescription.FillAllControlls(goodsName);
            }
        }

        //Событие добавления нового покупателя
        private void _BuyerAdded(object o, RoutedEventArgs eve)
        {
            expedintureCreator.FillComboBuyers(comboBuyers, false);

            comboBuyers.SelectedItem = buyerAdd.Buyer;

            BuyerAdded?.Invoke(o, eve);
        }

        //Заполнение всех полей
        private void FillAllControlls(bool withSelectBuyer)
        {
            string command = "SELECT MAX([InvoiceNumber]) FROM [ExpedintureInvoices]";
            expedintureCreator.FillInvoiceNumber(command, lblExpedintureNumber);

            expedintureCreator.FillComboBuyers(comboBuyers, withSelectBuyer);

            comboWarehouse.SelectionChanged -= new SelectionChangedEventHandler(comboWarehouse_SelectionChanged);            
            expedintureCreator.FillComboWarehouses(comboWarehouse);
            comboWarehouse.SelectionChanged += new SelectionChangedEventHandler(comboWarehouse_SelectionChanged);

            expedintureCreator.FillComboGoodsGroups(comboGroups, true);
        }

        private void FillComboGoods()
        {
            string command;
            if (comboGroups.SelectedItem.ToString().Equals("Все группы"))
                command = "SELECT [Goods].[GoodsName] FROM [Goods] " +
                    "INNER JOIN [GoodsInWarehouses] " +
                    "ON [Goods].[GoodsID] = [GoodsInWarehouses].[GoodsName] " +
                    "INNER JOIN [Warehouses] " +
                    "ON [GoodsInWarehouses].[WarehouseName] = [Warehouses].[WarehouseID] " +
                    "WHERE [Warehouses].[WarehouseName] = '" + comboWarehouse.SelectedItem.ToString() + "' " +
                    "AND [GoodsInWarehouses].[GoodsBalance] > 0";
            else command = "SELECT [Goods].[GoodsName] FROM [Goods] " +
                    "INNER JOIN [GoodsGroups] " +
                    "ON [Goods].[GoodsGroup] = [GoodsGroups].[GroupID] " +
                    "INNER JOIN [GoodsInWarehouses] " +
                    "ON [Goods].[GoodsID] = [GoodsInWarehouses].[GoodsName] " +
                    "INNER JOIN [Warehouses] " +
                    "ON [GoodsInWarehouses].[WarehouseName] = [Warehouses].[WarehouseID] " +
                    "WHERE [GoodsGroups].[GroupName] = '" + comboGroups.SelectedItem.ToString() + "' " +
                    "AND [Warehouses].[WarehouseName] = '" + comboWarehouse.SelectedItem.ToString() + "' " +
                    "AND [GoodsInWarehouses].[GoodsBalance] > 0";
            expedintureCreator.FillComboGoods(comboGoods, command);
        }


        //Изменение выбранной группы товаров
        private void ComboGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboGroups.SelectedItem != null)
            {
                if (comboGroups.Items[0].Equals("Выберите группу"))
                {
                    int selectedIndex = comboGroups.SelectedIndex - 1;
                    expedintureCreator.FillComboGoodsGroups(comboGroups, false);

                    comboGroups.SelectedIndex = selectedIndex;
                }
                FillComboGoods();
            }
        }

        private void FillInfoControlsBuyerGoodsInSale(string sellerName)
        {
            string command = "SELECT [Goods].[GoodsName] FROM [Goods] " +
                "INNER JOIN [GoodsInSale] " +
                "ON [GoodsInSale].[GoodsName] = [Goods].[GoodsID] " +
                "INNER JOIN [Buyers] " +
                "ON [GoodsInSale].[BuyerName] = [Buyers].[BuyerID] " +
                "WHERE [Buyers].[BuyerName] = '" + sellerName + "' " +
                "AND [GoodsInSale].[GoodsQuantity] > 0";

            expedintureCreator.FillComboGoods(comboBuyerGoodsInSale, command);

            if (comboBuyerGoodsInSale.Items.Count > 0)
            {
                lblBuyerHaveGoodsInSale.Visibility = Visibility.Visible;
                btnAddGoodsFromSale.IsEnabled = true;
            }
            else
            {
                lblBuyerHaveGoodsInSale.Visibility = Visibility.Hidden;
                btnAddGoodsFromSale.IsEnabled = false;
            }
        }

        //Изменение выбранного покупателя, заполнение комбо с товарами, которые на реализации
        private void ComboBuyers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboBuyers.SelectedItem != null)
            {
                if (comboBuyers.Items[0].ToString().Equals("Выберите покупателя"))
                {
                    int selectedIndex = comboBuyers.SelectedIndex - 1;
                    expedintureCreator.FillComboBuyers(comboBuyers, false);

                    comboBuyers.SelectedIndex = selectedIndex;
                }

                string sellerName = comboBuyers.SelectedItem.ToString();
                lblSellerName.Content = sellerName;

                FillInfoControlsBuyerGoodsInSale(sellerName);
            }
        }

        //Изменение выбранного товара на реализации, заполнение остатка, стоимости
        private void comboBuyerGoodsInSale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBuyerGoodsInSale.SelectedItem != null)
            {
                string goodsName = comboBuyerGoodsInSale.SelectedItem.ToString();
                string sellerName = lblSellerName.Content.ToString();

                string command = "SELECT [GoodsInSale].[GoodsQuantity], [GoodsInSale].[GoodsPrice] " +
                    "FROM [GoodsInSale] " +
                    "INNER JOIN [Goods] " +
                    "ON [GoodsInSale].[GoodsName] = [Goods].[GoodsID] " +
                    "INNER JOIN [Buyers] " +
                    "ON [GoodsInSale].[BuyerName] = [Buyers].[BuyerID] " +
                    "WHERE [Goods].[GoodsName] = '" + goodsName + "' " +
                    "AND [Buyers].[BuyerName] = '" + sellerName + "'";
                                
                    if (tabItem.Name == "itemSaleFromSeller")
                        goodsDescription.FillAllControlls(goodsName);

                new LabelFiller(this.sqlConnect).Fill(command, lblGoodsBalanceInSale, lblSalePrice);

                lblSalePrice.Content = StringTransformer.TransformDecimal(lblSalePrice.Content.ToString());

                tbGoodsPriceInSale.Text = lblSalePrice.Content.ToString();

                expedintureCreator.FillLblMeasure(goodsName, lblMeasureBalanceInSale);

                lblMeasureBalanceInSale.Content = lblMeasureBalanceInSale.Content;
            }
            else
            {
                if (tabItem.Name == "itemSaleFromSeller")
                    goodsDescription.FillAllControlls(string.Empty);

                lblSalePrice.Content = "-";
                tbGoodsQauntityInSale.Text = string.Empty;
                tbGoodsPriceInSale.Text = string.Empty;
                lblMeasureBalanceInSale.Content = string.Empty;
                lblGoodsBalanceInSale.Content = "-";
            }
        }

        //Изменение выбора склада
        private void comboWarehouse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboWarehouse.SelectedItem != null && !comboGroups.Items[0].Equals("Выберите группу"))
            {
                FillComboGoods();
            }
        }

        //Изменение выбранного товара
        private void comboGoods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboGoods.SelectedItem != null)
            {
                string goodsName = comboGoods.SelectedItem.ToString();

                SelectPercentAge(goodsName);
                                
                string command = "SELECT [GoodsInWarehouses].[GoodsBalance] FROM [GoodsInWarehouses] " +
                    "WHERE [GoodsInWarehouses].[GoodsName] = '" +
                    expedintureCreator.GetGoodsID(goodsName) + "' " +
                    "AND [GoodsInWarehouses].[WarehouseName] = '" +
                    expedintureCreator.GetWarehouseID(comboWarehouse.SelectedItem.ToString()) + "'";
                new LabelFiller(this.sqlConnect).Fill(lblGoodsBalance, command);
                

                command = "SELECT SUM([GoodsInWarehouses].[GoodsBalance]) FROM [GoodsInWarehouses] " +
                    "WHERE [GoodsInWarehouses].[GoodsName] = '" +
                    expedintureCreator.GetGoodsID(goodsName) + "' ";
                new LabelFiller(this.sqlConnect).Fill(lblTotalGoodsBalance, command);
               
                expedintureCreator.FillLblMeasure(goodsName, lblMeasureBalance);

                expedintureCreator.FillLblMeasure(goodsName, lblMeasurePrice);

                expedintureCreator.FillLblMeasure(goodsName, lblMeasureTotal);

                goodsDescription.FillAllControlls(goodsName);
            }
            else
            {
                lblGoodsBalance.Content = "-";
                lblMeasureBalance.Content = "-";
                lblMeasurePrice.Content = "-";
                lblMeasureTotal.Content = "-";
                lblTotalGoodsBalance.Content = "-";
                lblReccomendPrice.Content = "- руб.";

                goodsDescription.FillAllControlls(string.Empty);
            }
        }

        //Выбор между оптом и розницей
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

        enum ListAction
        {
            AddToListFromWarehouse,
            AddToListFromSeller,
            DeleteFromList,
            EditList
        }

        private void FillGridExpedinture(ListAction action)
        {
            if (action == ListAction.AddToListFromWarehouse || action == ListAction.AddToListFromSeller)
            {
                string goodsName = action == ListAction.AddToListFromWarehouse ?
                    comboGoods.SelectedItem.ToString() : comboBuyerGoodsInSale.SelectedItem.ToString();

                string goodsSource = action == ListAction.AddToListFromWarehouse ?
                        comboWarehouse.SelectedItem.ToString() : lblSellerName.Content.ToString();

                for (int i = 0; i < invoiceList.Count; i++)
                {
                    if (goodsName.Equals(invoiceList[i].GoodsName) && goodsSource.Equals(invoiceList[i].GoodsSource))
                    {
                        MessageBox.Show("Данный товар уже есть в списке. Для изменения количества или цены дважды кликните в соответствующем поле",
                            "Найдено совпадение", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }                            

                float goodsPrice = action == ListAction.AddToListFromWarehouse ?
                    float.Parse(tbGoodsPrice.Text) : float.Parse(tbGoodsPriceInSale.Text);

                float goodsQuantity = action == ListAction.AddToListFromWarehouse ?
                    (float.Parse(float.Parse(tbGoodsQauntity.Text) > float.Parse(lblGoodsBalance.Content.ToString()) ?
                    lblGoodsBalance.Content.ToString() : tbGoodsQauntity.Text)) :
                    (float.Parse(float.Parse(tbGoodsQauntityInSale.Text) > float.Parse(lblGoodsBalanceInSale.Content.ToString()) ?
                    lblGoodsBalanceInSale.Content.ToString() : tbGoodsQauntityInSale.Text));

                invoiceList.Add(new ExpedintureInvoiceContent
                {
                    RowNumber = invoiceList.Count + 1,
                    GoodsName = goodsName,
                    GoodsPrice = StringTransformer.TransformDecimal(goodsPrice.ToString()),
                    GoodsQuantity = goodsQuantity.ToString(),
                    MeasureUnit = lblMeasureBalance.Content.ToString(),
                    GoodsCost = StringTransformer.TransformDecimal((goodsPrice * goodsQuantity).ToString()),
                    GoodsSource = goodsSource
                });
            }

            else if (action == ListAction.DeleteFromList)
            {
                invoiceList.RemoveAt(gridExpedinture.SelectedIndex);
                for (int i = 0; i < invoiceList.Count; i++)
                {
                    invoiceList[i].RowNumber = i + 1;
                }
            }

            else if (action == ListAction.EditList)
            {

            }

            gridExpedinture.ItemsSource = null;
            gridExpedinture.ItemsSource = invoiceList;

            float totalSum = 0;
            if (invoiceList.Count > 0)
            {
                
                for (int i = 0; i < invoiceList.Count; i++)
                {
                    totalSum += float.Parse(invoiceList[i].GoodsCost);
                }                
            }
            lblExpedintureTotalSum.Content = StringTransformer.TransformDecimal(totalSum.ToString());
        }

        //Добавление строки товара в таблицу со склада
        private void BtnAddGoodsInGrid_Click(object sender, RoutedEventArgs e)
        {
            bool goodsNameIsFilled = comboGoods.SelectedItem != null ? true : false;
            bool goodsQuantityIsFilled = !tbGoodsQauntity.Text.Equals(string.Empty) ? true : false;
            bool goodsPriceIFilled = !tbGoodsPrice.Text.Equals(string.Empty) ? true : false;
            bool groupIsSelected = !comboGroups.SelectedItem.Equals("Выберите группу") ? true : false;

            if (goodsNameIsFilled && goodsQuantityIsFilled && goodsPriceIFilled && groupIsSelected)
            {
                FillGridExpedinture(ListAction.AddToListFromWarehouse);
                comboWarehouse.IsEnabled = false;
            }
            else ErrorMessages.FieldsNotCompleted();
        }

        //Добавление строки товара в таблицу от реализатора
        private void BtnAddGoodsInGridFromSeller_Click(object sender, RoutedEventArgs e)
        {
            bool goodsQuantityIsFilled = !tbGoodsQauntityInSale.Text.Equals(string.Empty) ? true : false;
            bool goodsPriceIsFilled = !tbGoodsPriceInSale.Text.Equals(string.Empty) ? true : false;

            if (goodsQuantityIsFilled && goodsPriceIsFilled)
            {
                FillGridExpedinture(ListAction.AddToListFromSeller);
                comboBuyers.IsEnabled = false;
            }

            else ErrorMessages.FieldsNotCompleted();
        }


        //Редактор ввода
        //--------------

        //Редактирование таблицы
        private void GridExpedinture_BeginingEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Column.DisplayIndex != 2 && e.Column.DisplayIndex != 3)
            {
                e.Cancel = true;
            }
        }

        private void GridExpedinture_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                TextBox tb = e.EditingElement as TextBox;

                TextHandler.DotReplace(tb);

                float temp = float.Parse(tb.Text);                
                
                int index = gridExpedinture.SelectedIndex;

                switch (e.Column.SortMemberPath)
                {
                    case "GoodsQuantity":
                        {
                            ExpedintureInvoiceContent gridRow = gridExpedinture.SelectedItem as ExpedintureInvoiceContent;

                            string goodsName = gridRow.GoodsName;
                            string goodsSource = gridRow.GoodsSource;//;

                            string command = string.Empty;

                            float goodsQuantity = 0;

                            //Определение возможного колмчества товаров
                            if (goodsSource.Equals(comboWarehouse.SelectedItem.ToString()))

                            {
                                goodsQuantity = expedintureCreator.GetGoodsBalanceInWarehouse(goodsName, goodsSource);
                            }

                            else if(goodsSource.Equals(comboBuyers.SelectedItem.ToString()))
                            {
                                goodsQuantity = expedintureCreator.GetGoodsBalanceInSaleFromBuyer(goodsName, goodsSource);
                            }                            

                            invoiceList[index].GoodsQuantity =
                                goodsQuantity >= temp ? temp.ToString() : goodsQuantity.ToString();
                        }
                        break;
                    case "GoodsPrice":
                        invoiceList[index].GoodsPrice = StringTransformer.TransformDecimal(temp.ToString());
                        break;
                }


                invoiceList[index].GoodsCost =
                    StringTransformer.TransformDecimal((float.Parse(invoiceList[index].GoodsPrice) *
                    float.Parse(invoiceList[index].GoodsQuantity)).ToString());                
            }

            catch
            {
                MessageBox.Show("Изменения не были применены. Введите корректные данные",
                    "Некорректные данные", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            finally
            {
                FillGridExpedinture(ListAction.EditList);
            }
        }

        //Редактирование стоимости товара
        private void tbGoodsPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextHandler.DotReplace(tbGoodsPrice);
        }

        //Редактирование количества
        private void tbGoodsQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextHandler.DotReplace(tbGoodsQauntity);
        }


        //Фильтр ввода
        //------------

        //Фильтрация ввода символов
        private void PrewiewTextInput(object sender, TextCompositionEventArgs e)
        { 
            TextBox textBox = sender as TextBox;
            string text = textBox != null ? textBox.Text : string.Empty;
            e.Handled = TextHandler.Filter(e.Text, text);
        }

        //Фильтрация ввода пробела
        private void TextPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        //Удаление строки товара из таблицы
        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (gridExpedinture.SelectedItem != null)
            {
                FillGridExpedinture(ListAction.DeleteFromList);

                int countOfWarehouseGoods = 0;
                int countOfBuyersGoods = 0;

                for (int i = 0; i < invoiceList.Count; i++)
                {
                    if (invoiceList[i].GoodsSource == comboWarehouse.SelectedItem.ToString()) countOfWarehouseGoods++;
                    else countOfBuyersGoods++;
                }

                if (countOfWarehouseGoods == 0) comboWarehouse.IsEnabled = true;

                if (countOfBuyersGoods == 0) comboBuyers.IsEnabled = true;
            }
        }

        //Создание накладной
        private void btnCreatePurchaseInvoice_Click(object sender, RoutedEventArgs e)
        {
            bool buyerIsSelected = 
                !comboBuyers.SelectedItem.ToString().Equals("Выберите покупателя") ? true : false;
            bool tableIsFiled = invoiceList.Count > 0 ? true : false;


            if (buyerIsSelected && tableIsFiled)
            {
                DataBaseWriter insertData = new DataBaseWriter(this.sqlConnect);

                //Таблица расходных накладных
                //Номер накладной
                insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "InvoiceNumber", Data = expedintureCreator.InvoiceNumber });

                //Дата создания
                insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "CreateDate", Data = Convert.ToDateTime(DateTime.Now).ToString("yyyyMMdd") });

                //Склад-продавец
                string warehouseName = comboWarehouse.SelectedItem.ToString();
                int _WarehouseID = expedintureCreator.GetWarehouseID(warehouseName);
                insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "WarehouseName", Data = _WarehouseID });

                //Покупатель                
                string buyerName = comboBuyers.SelectedItem.ToString();
                int _BuyerID = expedintureCreator.GetBuyerID(buyerName);
                insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "BuyerName", Data = _BuyerID });

                //Общая сумма
                insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "TotalSum", Data = TextHandler.FloatToString(float.Parse(lblExpedintureTotalSum.Content.ToString())) });

                string command =
                    "INSERT INTO [ExpedintureInvoices] (InvoiceNumber, CreateDate, BuyerName, WarehouseName, TotalSum)" +
                    "VALUES(@InvoiceNumber, @CreateDate, @BuyerName, @WarehouseName, @TotalSum)";
                insertData.WriteData(command);

                //Таблица детализации расходной накладной
                command = "SELECT [InvoiceID] FROM [ExpedintureInvoices] " +
                    "WHERE [InvoiceNumber] = '" + expedintureCreator.InvoiceNumber + "'";
                int invoiceID = expedintureCreator.GetInvoiceID(command);

                for (int i = 0; i < invoiceList.Count; i++)
                {
                    insertData.InsDataRow = new List<InsertDataToRow>();

                    //ID накладной                                        
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "InvoiceNumber", Data = invoiceID });

                    //ID товара 
                    string goodsName = invoiceList[i].GoodsName;
                    int _GoodsID = expedintureCreator.GetGoodsID(goodsName);
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsName", Data = _GoodsID });

                    //Количество единиц
                    float _GoodsQauntity = float.Parse(invoiceList[i].GoodsQuantity);
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsQuantity", Data = TextHandler.FloatToString(_GoodsQauntity) });

                    //Цена единицы
                    float _GoodsPrice = float.Parse(invoiceList[i].GoodsPrice);
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsPrice", Data = TextHandler.FloatToString(_GoodsPrice) });

                    //Стоимость
                    float _GoodsCost = float.Parse(invoiceList[i].GoodsCost);
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsCost", Data = TextHandler.FloatToString(_GoodsCost) });

                    command = "SELECT COUNT([ExpedintureInvoicesContent].[GoodsQuantity]) FROM [ExpedintureInvoicesContent] " +
                        "WHERE [ExpedintureInvoicesContent].[GoodsName] = '" + _GoodsID + "' " +
                        "AND [ExpedintureInvoicesContent].[InvoiceNumber] = '" + invoiceID + "'";
                    float wroteGoodsQuantity = float.Parse(new DataBaseReader(this.sqlConnect).GetString(command));

                    if (wroteGoodsQuantity > 0)
                    {
                        float newQuantity = _GoodsQauntity + wroteGoodsQuantity;
                        command = 
                            "UPDATE [ExpedintureInvoicesContent]" +
                            "SET [GoodsQuantity] = '" + newQuantity + "' " +
                            "WHERE [ExpedintureInvoicesContent].[GoodsName] = '" + _GoodsID + "' " +
                            "AND [ExpedintureInvoicesContent].[InvoiceNumber] = '" + invoiceID + "'";
                    }
                    else
                    {
                        command =
                          "INSERT INTO [ExpedintureInvoicesContent] (InvoiceNumber, GoodsName, GoodsQuantity, GoodsPrice, GoodsCost) " +
                          "VALUES (@InvoiceNumber, @GoodsName, @GoodsQuantity, @GoodsPrice, @GoodsCost)";
                    }

                    insertData.WriteData(command);                    
                    
                    //Определение источника товара

                    //Источник - склад
                    if (invoiceList[i].GoodsSource == warehouseName)
                    {
                        //Таблица ТОВАРЫ НА СКЛАДЕ                       

                        //Остаток на складе
                        float _GoodsOldQuantity = expedintureCreator.GetGoodsBalanceInWarehouse(goodsName, warehouseName);

                        //Новый остаток на складе
                        float _GoodsNewQuantity = _GoodsOldQuantity - _GoodsQauntity;

                        command = "UPDATE [GoodsInWarehouses] " +
                            "SET [GoodsBalance] = " + TextHandler.FloatToString(_GoodsNewQuantity) + " " +
                            "WHERE [GoodsName] = '" + _GoodsID + "'" +
                            "AND [WarehouseName] = '" + _WarehouseID + "'";

                        new DataBaseWriter(this.sqlConnect).WriteData(command);
                    }

                    //Источник - продавец
                    else if(invoiceList[i].GoodsSource == buyerName)
                    {
                        //Таблица ТОВАРЫ НА РЕАЛИЗАЦИИ

                        //Остаток у реализатора
                        float _GoodsOldQuantity = expedintureCreator.GetGoodsBalanceInSaleFromBuyer(goodsName, buyerName);

                        //Новый остаток на реализации
                        float _GoodsNewQuantity = _GoodsOldQuantity - _GoodsQauntity;

                        command = "UPDATE [GoodsInSale] " +
                            "SET [GoodsQuantity] = " + TextHandler.FloatToString(_GoodsNewQuantity) + " " +
                            "WHERE [GoodsName] = '" + _GoodsID + "' " +
                            "AND [BuyerName] = '" + _BuyerID + "'";

                        new DataBaseWriter(this.sqlConnect).WriteData(command);
                    }
                }

                //Удаление необработанной накладной, если это ее завершение
                if(unworkInfo != null)
                {
                    IExpedintureUnwork unwork = new InvoiceCreator(this.sqlConnect);
                    unwork.DeleteUnworkInvoice(unworkInfo.InvoiceID);
                }

                MessageBox.Show(string.Format("Расходная накладная №{0} успешно создана", expedintureCreator.InvoiceNumber),
                    "Выполнено", MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }

            else if(!buyerIsSelected)
            {
                MessageBox.Show("Выберите покупателя из списка или создайте нового",
                    "Продавец не выбран", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            else if(!tableIsFiled)
            {
                MessageBox.Show("Заполните таблицу детализации накладной", "Таблица пуста", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Создание необработанной накладной
        private void BtnCreateUnworked_Click(object sender, RoutedEventArgs e)
        {
            bool buyerIsSelected =
                !comboBuyers.SelectedItem.ToString().Equals("Выберите покупателя") ? true : false;
            bool tableIsFiled = invoiceList.Count > 0 ? true : false;
            
            if (buyerIsSelected && tableIsFiled)
            {
                DataBaseWriter insertData = new DataBaseWriter(this.sqlConnect);

                //Таблица расходных накладных                
                //Склад-продавец
                string warehouseName = comboWarehouse.SelectedItem.ToString();
                int _WarehouseID = expedintureCreator.GetWarehouseID(warehouseName);
                insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "WarehouseName", Data = _WarehouseID });

                //Покупатель                
                string buyerName = comboBuyers.SelectedItem.ToString();
                int _BuyerID = expedintureCreator.GetBuyerID(buyerName);
                insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "BuyerName", Data = _BuyerID });

                //Общая сумма
                insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "TotalSum", Data = TextHandler.FloatToString(float.Parse(lblExpedintureTotalSum.Content.ToString())) });

                string command =
                    "INSERT INTO [ExpedintureUnwork] (BuyerName, WarehouseName, TotalSum)" +
                    "VALUES(@BuyerName, @WarehouseName, @TotalSum)";
                insertData.WriteData(command);

                //Таблица детализации расходной накладной
                command = "SELECT MAX([InvoiceID]) FROM [ExpedintureUnwork]";
                int invoiceID = expedintureCreator.GetInvoiceID(command);
                for (int i = 0; i < invoiceList.Count; i++)
                {
                    insertData.InsDataRow = new List<InsertDataToRow>();

                    //ID накладной                                        
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "InvoiceNumber", Data = invoiceID });

                    //ID товара 
                    string goodsName = invoiceList[i].GoodsName;
                    int _GoodsID = expedintureCreator.GetGoodsID(goodsName);
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsName", Data = _GoodsID });

                    //Количество единиц
                    float _GoodsQauntity = float.Parse(invoiceList[i].GoodsQuantity);
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsQuantity", Data = TextHandler.FloatToString(_GoodsQauntity) });

                    //Цена единицы
                    float _GoodsPrice = float.Parse(invoiceList[i].GoodsPrice);
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsPrice", Data = TextHandler.FloatToString(_GoodsPrice) });

                    //Стоимость
                    float _GoodsCost = float.Parse(invoiceList[i].GoodsCost);
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsCost", Data = TextHandler.FloatToString(_GoodsCost) });

                    //Определение источника товара
                    string goodsSource = "";
                    //Источник - склад
                    if (invoiceList[i].GoodsSource == warehouseName)
                    {
                        goodsSource = "w";
                    }

                    //Источник - продавец
                    else if (invoiceList[i].GoodsSource == buyerName)
                    {
                        goodsSource = "b";
                    }
                    insertData.InsDataRow.Add(new InsertDataToRow { ColumnName = "GoodsSource", Data = goodsSource });

                    command =
                        "INSERT INTO [ExpedintureUnworkContent] (InvoiceNumber, GoodsName, GoodsQuantity, GoodsPrice, GoodsCost, GoodsSource) " +
                        "VALUES (@InvoiceNumber, @GoodsName, @GoodsQuantity, @GoodsPrice, @GoodsCost, @GoodsSource)";
                    insertData.WriteData(command);

                }

                if (unworkInfo != null)
                {
                    IExpedintureUnwork unwork = new InvoiceCreator(this.sqlConnect);
                    unwork.DeleteUnworkInvoice(unworkInfo.InvoiceID);
                }

                MessageBox.Show("Расходная накладная успешно добавлена в список необработанных накладных",
                    "Выполнено", MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }

            else if (!buyerIsSelected)
            {
                MessageBox.Show("Выберите покупателя из списка или создайте нового",
                    "Продавец не выбран", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            else if (!tableIsFiled)
            {
                MessageBox.Show("Заполните таблицу детализации накладной", "Таблица пуста",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
