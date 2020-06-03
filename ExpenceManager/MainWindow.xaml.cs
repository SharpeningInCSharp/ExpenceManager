﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DiagramControls;
using DiagramModel;
using ExpenceManager.AdditionalContrils;
using Microsoft.Win32;
using Model;
using Model.DataBase;
using Utils.InputUtils;
using Utils.Managers;

namespace ExpenceManager
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private const string SampleExamplesDir = @"C:\Users\aleks\source\repos\ExpenceManager\Purchase txt samples";
		private SolidColorBrush[] brushes = new SolidColorBrush[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Purple, Brushes.Cyan, Brushes.Orange };

		private Purchase purchase;
		private PieDiagram pie;

		private readonly Func<IEnumerable<GoodType>> typesProvider;
		private readonly Func<GoodType, DateTime, DateTime?, IEnumerable<PurchaseItem>> dataProvider;

		public DateTime InitialDate => new DateTime(2018, 1, 1);

		public MainWindow()
		{
			InitializeComponent();

			typesProvider = PurchaseDB.GetAllGoodTypes;
			dataProvider = PurchaseDB.GetItemsByTypeAtDateRange;

			Initialize();
		}

		private void Initialize()
		{
			var lastDate = PurchaseDB.GetLastPurchaseDate();
			ManagerCalendar.DisplayDateEnd = DateTime.Today;
			ManagerCalendar.DisplayDateStart = InitialDate;

			if (lastDate != null)
			{
				InitializeDiagram();
				ManagerCalendar.SelectedDate = lastDate;
				Task.Run(() => CrossOutUnavailableDates());
			}
		}

		private void CrossOutUnavailableDates()
		{
			var availableDates = PurchaseDB.GetAvailableDates();
			Dispatcher.Invoke(() => ManagerCalendar.BlackoutDates.Clear());

			var currentDate = DateTime.Today;
			while (currentDate != InitialDate)
			{
				if (availableDates.Count(x => x.Year == currentDate.Year &&
										x.Month == currentDate.Month &&
										x.Day == currentDate.Day) == 0)     // if there's no such date
				{
					Dispatcher.Invoke(() => ManagerCalendar.BlackoutDates.Add(new CalendarDateRange(currentDate)));
				}

				currentDate = currentDate.AddDays(-1);
			}
		}

		private void InitializeDiagram()
		{
			var scopes = new Scopes<GoodType, PurchaseItem>(typesProvider, dataProvider, DateTime.Today, null);

			if (scopes.NotEmptyScopesAmount > brushes.Length)
				CreateBrushes(scopes.NotEmptyScopesAmount);

			pie = new PieDiagram(scopes, brushes);

			PlacePie();
		}

		private void CreateBrushes(int scopesAmount)
		{
			var rnd = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray()));

			var d = scopesAmount - brushes.Length;
			Array.Resize(ref brushes, brushes.Length + d);

			while (d > 0)
			{
				var color = Color.FromRgb((byte)rnd.Next(byte.MaxValue),
											(byte)rnd.Next(byte.MaxValue),
											(byte)rnd.Next(byte.MaxValue));

				if (IsSimular(color) == false)
				{
					brushes[^d] = new SolidColorBrush(color);
				}

				d--;
			}
		}

		private bool IsSimular(Color color)
		{
			for (int i = 0; i < brushes.Length; i++)
			{
				if (brushes[i] != null)
				{
					if (Color.AreClose(brushes[i].Color, color))
						return true;
				}
			}

			return false;
		}

		private void PlacePie()
		{
			Grid.SetColumn(pie, 2);
			Grid.SetRow(pie, 0);
			MainGrid.Children.Add(pie);
		}

		private void LoadNewPie(DateTime initialDate, DateTime? finalDate)
		{
			Task.Run(() => CrossOutUnavailableDates());

			Dispatcher?.Invoke(() =>
			{
				if (pie is null)
				{
					var scopes = new Scopes<GoodType, PurchaseItem>(typesProvider, dataProvider, initialDate, finalDate);

					if (scopes.NotEmptyScopesAmount > brushes.Length)
						CreateBrushes(scopes.NotEmptyScopesAmount);

					pie = new PieDiagram(scopes, brushes);

					PlacePie();
				}
				else
				{
					var scopes = new Scopes<GoodType, PurchaseItem>(typesProvider, dataProvider, initialDate, finalDate);

					if (scopes.NotEmptyScopesAmount > brushes.Length)
						CreateBrushes(scopes.NotEmptyScopesAmount);

					pie.LoadNew(scopes);
				}
			});
		}

		private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
		{
			MainGrid.Focus();
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			var purchaseWin = new AddPurchaseWindow();
			purchaseWin.PurchaseCreated += PurchaseWin_PurchaseCreated;
			purchaseWin.ShowDialog();
		}

		private void PurchaseWin_PurchaseCreated(Purchase obj)
		{
			purchase = obj;
			LoadNewPie(purchase.Date, null);
		}

		private void LoadFromComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (LoadFromComboBox.SelectedItem != null)
			{
				var itemTag = ((ComboBoxItem)LoadFromComboBox.SelectedItem).Tag;

				LoadFromComboBox.SelectedIndex = -1;
				switch (itemTag)
				{
					case "TXT":
						OpenTxtFileDialog();
						break;

					case "EXCEL":
						OpenExcelFileDialog();
						break;

					default:
						throw new ArgumentException("Can't recognize the tag");
				}
			}
		}

		private void OpenExcelFileDialog()
		{
			const string txtExtension = ".xlsx";

			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				InitialDirectory = @"C:\Users\aleks\source\repos\ExpenceManager\Purchase xlsx samples",
			};
			openFileDialog.ShowDialog();

			var fileName = openFileDialog.FileName;
			if (fileName.EndsWith(txtExtension))
			{
				ReadXlsxAsync(fileName);
			}
			else
			{
				MessageBox.Show("Please select Excel(.xlsx) file.");
			}
		}

		private async void ReadXlsxAsync(string fileName)
		{
			var runningTask = Task.Run(() => ReadXlsxFile(fileName));
			await Task.Run(() => LoadingAnimation(runningTask));
		}

		private void ReadXlsxFile(string fileName)
		{
			try
			{
				var items = ExcelFileManager.Read(fileName);

				DateTime initialDate = items.First().Date;
				DateTime? finalDate = items.Last().Date;
				foreach (var item in items)
				{
					PurchaseDB.Add(item);
				}

				if (initialDate == finalDate.Value)
					finalDate = null;

				LoadNewPie(initialDate, finalDate);
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(() => MessageBox.Show(ex.Message));
			}
		}

		private void OpenTxtFileDialog()
		{
			const string txtExtension = ".txt";

			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				InitialDirectory = SampleExamplesDir,
			};
			openFileDialog.ShowDialog();

			var fileName = openFileDialog.FileName;
			if (fileName.EndsWith(txtExtension))
			{
				ReadTxtAsync(fileName);
			}
			else
			{
				MessageBox.Show("Please select txt file.");
			}
		}

		private async void ReadTxtAsync(string fileName)
		{
			var runningTask = Task.Run(() => ReadTxtFile(fileName));
			await Task.Run(() => LoadingAnimation(runningTask));
		}

		private void LoadingAnimation(Task runningTask)
		{
			const int delayTime = 300;
			Dispatcher.Invoke(() => LoadingTextBlockAnimation.Visibility = Visibility.Visible);

			while (runningTask.Status != TaskStatus.RanToCompletion &&
					runningTask.Status != TaskStatus.Faulted)
			{
				Dispatcher.Invoke(() => LoadingTextBlockAnimation.Text = "Loading");
				Thread.Sleep(delayTime);

				Dispatcher.Invoke(() => LoadingTextBlockAnimation.Text = "Loading.");
				Thread.Sleep(delayTime);

				Dispatcher.Invoke(() => LoadingTextBlockAnimation.Text = "Loading..");
				Thread.Sleep(delayTime);

				Dispatcher.Invoke(() => LoadingTextBlockAnimation.Text = "Loading...");
				Thread.Sleep(delayTime);
			}

			Dispatcher.Invoke(() => LoadingTextBlockAnimation.Visibility = Visibility.Hidden);
		}

		private void ReadTxtFile(string fileName)
		{
			try
			{
				var date = File.GetLastWriteTime(fileName);
				purchase = new Purchase(date);

				foreach (var item in GoodsTextParser.Read(fileName))
				{
					purchase.Add(item);
				}

				PurchaseDB.Add(purchase);

				LoadNewPie(date, null);
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(() => MessageBox.Show(ex.Message));
			}
		}

		private void ExportComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ExportComboBox.SelectedItem != null)
			{
				var itemTag = ((ComboBoxItem)ExportComboBox.SelectedItem).Tag;

				ExportComboBox.SelectedIndex = -1;
				switch (itemTag)
				{
					case "EXCEL":
						CreateExcelFile();
						break;

					default:
						throw new ArgumentException("Can't recognize the tag");
				}
			}
		}

		private async void CreateExcelFile()
		{
			GetDates(out DateTime initialDate, out DateTime? finalDate);

			await Task.Run(() => ExcelFileManager.Create(initialDate, finalDate));
		}

		private void ManagerCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
		{
			if (pie != null)
			{
				Mouse.Capture(null);

				GetDates(out DateTime initialDate, out DateTime? finalDate);

				if (initialDate == finalDate.Value)
					finalDate = null;

				var sc = new Scopes<GoodType, PurchaseItem>(typesProvider, dataProvider, initialDate, finalDate);
				pie.LoadNew(sc);
			}
			else
			{
				Initialize();
			}
		}

		private void GetDates(out DateTime initialDate, out DateTime? finalDate)
		{
			var dates = ManagerCalendar.SelectedDates.OrderBy(x => x.Date);

			initialDate = dates.First();
			finalDate = dates.Last();
		}
	}
}
