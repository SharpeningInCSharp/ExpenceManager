﻿using DiagramModel;
using Model.DataBase;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Model
{
	public partial class Purchase : EnumerableType<GoodType>
	{
		public DateTime Date { get; }

		public decimal Sum => Goods.Sum(x => x.Key.Price);

		public int ItemsAmount => Goods.Count;

		public Dictionary<Good, int> Goods { get; }

		/// <summary>
		/// Creates Purchase object for today
		/// </summary>
		public Purchase()
		{
			types = new List<GoodType>();
			Date = DateTime.Now;
			Goods = new Dictionary<Good, int>();
		}

		/// <summary>
		/// Creares Purchase object for curtain date
		/// </summary>
		/// <param name="date"></param>
		public Purchase(DateTime date)
		{
			Date = date;
			types = new List<GoodType>();
			Goods = new Dictionary<Good, int>();
		}

		//TODO: might be unrequired
		internal Purchase(PurchaseEntity purchaseEntity)
		{
			Date = purchaseEntity.Date;
			Goods = ConvertToModelType(purchaseEntity.Goods);
			types = Goods.Select(x => x.Key.Type).Distinct().ToList();
		}

		private Dictionary<Good, int> ConvertToModelType(List<PurchaseItemEntity> goods)
		{
			var res = new Dictionary<Good, int>();

			foreach (var item in goods)
			{
				res.Add(new Good(item.Good), item.Amount);
			}

			return res;
		}

		public void Add(PurchaseItem item)
		{
			if (item != null)
			{
				var good = new Good(item.Name, item.Price, item.Type);

				if (Goods.ContainsKey(good))
				{
					Goods[good] += item.Amount;
				}
				else
				{
					Goods.Add(good, item.Amount);

					if (types.Contains(good.Type) == false)
						types.Add(good.Type);
				}
			}
		}

		public IEnumerable<PurchaseItem> GetPurchaseItems()
		{
			return Goods.Select(x => new PurchaseItem(x.Key.Name, x.Key.Price, x.Key.Type, x.Value));
		}


	}

	public partial class Purchase : IEnumerable<PurchaseItem>
	{

		//TODO: must return not good but PurchaseItem
		public IEnumerator<PurchaseItem> GetEnumerator()
		{
			return Goods.Select(x => new PurchaseItem(x.Key.Name, x.Key.Price, x.Key.Type, x.Value)).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Goods.Select(x => new PurchaseItem(x.Key.Name, x.Key.Price, x.Key.Type, x.Value)).GetEnumerator();
		}
	}

	/// <summary>
	/// Special item for Purchase selection. Model part
	/// </summary>
	public partial class PurchaseItem
	{
		public string Name { get; }
		public decimal Price { get; }
		public int Amount { get; internal set; }
		public GoodType Type { get; }

		private PurchaseItem(string name, decimal price, int amount)
		{
			Name = name;
			Price = price;
			Amount = amount;
		}

		public PurchaseItem(string name, decimal price, GoodType type, int amount) : this(name, price, amount)
		{
			Type = type;
		}

		public PurchaseItem(string name, decimal price, string type, int amount) : this(name, price, amount)
		{
			Type = new GoodType(type);
		}

		public override string ToString()
		{
			return $"{Amount} x {Name} - {Price:#0.00}";
		}
	}

	/// <summary>
	/// Interfaces implementation's part
	/// </summary>
	public partial class PurchaseItem : IScopeSelectionItem, IEquatable<PurchaseItem>
	{
		public decimal GetTotal => Price * Amount;

		public override bool Equals(object obj)
		{
			if (obj is PurchaseItem purchaseItem)
				return Equals(purchaseItem);

			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Price, Type);
		}

		public bool Equals(PurchaseItem other)
		{
			if (other is null)
				return false;

			return Name.Equals(other.Name)
					&& Price == other.Price
					&& Type.Equals(other.Type);
		}
	}
}
