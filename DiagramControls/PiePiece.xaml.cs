﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DiagramControls
{
	/// <summary>
	/// Логика взаимодействия для PiePiece.xaml
	/// </summary>
	public partial class PiePiece : UserControl
	{
		private double angle;
		public double Angle
		{
			get => angle;
			set
			{
				angle = value;
				Builder();
			}
		}

		private Brush defaultBrush;
		public Brush DefaultBrush
		{
			get => defaultBrush;
			set
			{
				MainPath.Fill = defaultBrush = value;
			}
		}

		public delegate void PiePieceHandler(PiePiece sender);
		public event PiePieceHandler MouseIn;
		public event PiePieceHandler MouseOut;

		public int Num { get; }
		public int Ind { get; }
		public Point Center { get; private set; } = new Point(0, 0);

		private const double factor = Math.PI / 180;

		private const double R = 200;

		public PiePiece()
		{
			InitializeComponent();
		}

		public PiePiece(int num, int ind, double angle, SolidColorBrush brush)
		{
			InitializeComponent();
			Num = num;
			Ind = ind;
			DefaultBrush = brush;
			Angle = angle;
		}

		private void Builder()
		{
			Geometry.Segments.Clear();
			bool isLargeArc = angle > 180;

			Geometry.Segments.Add(new LineSegment(new Point(Center.X, Center.Y - R), true));
			Geometry.Segments.Add(new ArcSegment(new Point(Center.X + R * Math.Sin(Angle * factor), Center.Y - R * Math.Cos(Angle * factor)),
								new Size(R, R),
								rotationAngle: 0, isLargeArc: isLargeArc, SweepDirection.Clockwise, true));
			Geometry.Segments.Add(new LineSegment(new Point(Center.X, Center.Y), true));
		}

		/// <summary>
		/// Visually select this item and display binding data
		/// </summary>
		public void Select()
		{
			MainPath.StrokeThickness = 3;
			//#E999FF
			MainPath.Fill = new SolidColorBrush(Color.FromArgb(255, 233, 153, 255));

			MouseIn?.Invoke(this);
		}

		/// <summary>
		/// Vosially unselect this item
		/// </summary>
		public void Unselect()
		{
			MainPath.StrokeThickness = 1;
			MainPath.Fill = DefaultBrush;

			MouseOut?.Invoke(this);
		}

		private void Path_MouseEnter(object sender, MouseEventArgs e)
		{
			Select();
		}

		private void Path_MouseLeave(object sender, MouseEventArgs e)
		{
			Unselect();
		}

		public void Rotate(double angle)
		{
			SectorRotation.Angle = angle;
		}
	}
}
