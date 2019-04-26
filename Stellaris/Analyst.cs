﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace StellarisLedger
{
	public class Analyst
	{
		private readonly ILogger logger = ApplicationLogging.CreateLogger<Analyst>();
		private readonly string content;
		private readonly IParseTree countriesData;
		private readonly IParseTree planetsData;
		private readonly IParseTree popData;
		private readonly IParseTree pop_factionsData;
		public string PlayerTag { get; }

		public Analyst(string content)
		{
			this.content = content;
			string lineEndingSymbol = content.DetectLineEnding();
			var p = content.IndexOf(lineEndingSymbol + "player={" + lineEndingSymbol);
			p += lineEndingSymbol.Length;
			content = content.Substring(p);

			ParadoxParser.ParadoxContext playerData = (ParadoxParser.ParadoxContext)GetScopeBody(content);
			var playerCountryTag = playerData.GetChild(0).GetChild(0).GetChild(1);
			PlayerTag = GetValue(playerCountryTag, "country").GetText();
			//+3是因为GetScopeBody返回children，不含右大括号。
			content = content.Substring(playerData.Stop.StopIndex + 3);


			p = content.IndexOf(lineEndingSymbol + "pop={" + lineEndingSymbol);
			p += lineEndingSymbol.Length;
			content = content.Substring(p);
			ParadoxParser.ParadoxContext popData = (ParadoxParser.ParadoxContext)GetScopeBody(content);
			this.popData = popData;
			//+3是因为GetScopeBody返回children，不含右大括号。
			content = content.Substring(popData.Stop.StopIndex + 3);

			p = content.IndexOf(lineEndingSymbol + "planet={" + lineEndingSymbol);
			p += lineEndingSymbol.Length;
			content = content.Substring(p);
			ParadoxParser.ParadoxContext planetsData = (ParadoxParser.ParadoxContext)GetScopeBody(content);
			this.planetsData = planetsData;
			//+3是因为GetScopeBody返回children，不含右大括号。
			content = content.Substring(planetsData.Stop.StopIndex + 3);

			p = content.IndexOf(lineEndingSymbol + "country={" + lineEndingSymbol);
			p += lineEndingSymbol.Length;
			content = content.Substring(p);
			ParadoxParser.ParadoxContext countriesData = (ParadoxParser.ParadoxContext)GetScopeBody(content);
			this.countriesData = countriesData;
			content = content.Substring(countriesData.Stop.StopIndex + 3);


			p = content.IndexOf(lineEndingSymbol + "pop_factions={" + lineEndingSymbol);
			if (p != -1)
			{
				//机械帝国没有派系
				p += lineEndingSymbol.Length;
				content = content.Substring(p);
				ParadoxParser.ParadoxContext pop_factionsData = (ParadoxParser.ParadoxContext)GetScopeBody(content);
				this.pop_factionsData = pop_factionsData;
				content = content.Substring(pop_factionsData.Stop.StopIndex + 3);
			}
		}

		public DateTimeOffset GetInGameDate()
		{
			var p = content.IndexOf("date=\"");
			if (p == -1)
				throw new FormatException("save game doesn't have date field.");
			p += "date=\"".Length;

			var p2 = content.IndexOf('"', p + 1);

			var s = content.Substring(p, p2 - p);

			return DateTimeOffset.ParseExact(s, "yyyy.MM.dd", CultureInfo.InvariantCulture.DateTimeFormat);
		}


		public IList<Country> GetCountries()
		{

			//Stopwatch sw = new Stopwatch();
			List<Country> countries = new List<Country>();
			for (int i = 0; i < countriesData.ChildCount; i++)
			{
				var country = new Country();

				var countryData = countriesData.GetChild(i);

				country.Tag = countryData.GetChild(0).GetText();
				var rightValue = countryData.GetChild(2);

				if (!(rightValue is ParadoxParser.ScopeContext))
					continue;

				if (new[] { "awakened_fallen_empire", "fallen_empire", "default" }.Contains(GetStringValue(rightValue.GetChild(1), "type")) == false)
					continue;
				PopulateCountry(country, (ParadoxParser.ParadoxContext)rightValue.GetChild(1));

				countries.Add(country);
			}

			//sw.Stop();
			//Console.WriteLine($"生成数据用时{sw.ElapsedMilliseconds}ms");

			return countries;
		}

		public Country GetCountry(string tag)
		{
			var country = new Country();
			country.Tag = tag;

			var countryData = GetValue(countriesData, tag);
			var scope = countryData as ParadoxParser.ScopeContext;
			if (scope == null)
				throw new ArgumentException($"{tag} is not a playable country.");

			PopulateCountry(country, scope.paradox());
			return country;
		}

		private void PopulateCountry(Country country, ParadoxParser.ParadoxContext kvPairs)
		{
			country.Name = GetStringValue(kvPairs, "name");

			country.TechnologyCount = GetTechnologyCount(kvPairs);

			country.MilitaryPower = Convert.ToDouble(GetValue(kvPairs, "military_power").GetChild(0).GetText());


			country.CivilianStations = GetValue(kvPairs, "controlled_planets").ChildCount - 2;

			var owned_planets = GetValue(kvPairs, "owned_planets");
			country.Colonies = (from id in GetColonies(owned_planets)
								select GetPlanet(id)).ToList();

			var modules = GetValue(kvPairs, "modules").GetChild(1);
			var standard_economy_module = GetValue(modules, "standard_economy_module").GetChild(1);
			var resources = GetValue(standard_economy_module, "resources").GetChild(1);
			var energy = GetValue(resources, "energy");
			if (energy is ParadoxParser.ScopeContext)
				country.Energy = Convert.ToDouble(energy.GetChild(1).GetText());
			else
				country.Energy = Convert.ToDouble(energy.GetText());

			var minerals = GetValue(resources, "minerals");
			if (minerals is ParadoxParser.ScopeContext)
				country.Minerals = Convert.ToDouble(minerals.GetChild(1).GetText());
			else
				country.Minerals = Convert.ToDouble(minerals.GetText());

			var food = GetValue(resources, "food");
			if (food is null)
				country.Food = 0;
			else if (food is ParadoxParser.ScopeContext)
				country.Food = Convert.ToDouble(food.GetChild(1).GetText());
			else
				country.Food = Convert.ToDouble(food.GetText());


			var influence = GetValue(resources, "influence");
			if (influence is ParadoxParser.ScopeContext)
				country.Influence = Convert.ToDouble(influence.GetChild(1).GetText());
			else
				country.Influence = Convert.ToDouble(influence.GetText());


			//var unity = GetValue(resources, "unity");
			//if (unity is ParadoxParser.ScopeContext)
			//    country.Unity = Convert.ToDouble(unity.GetChild(1).GetText());
			//else
			//    country.Unity = Convert.ToDouble(unity.GetText());

			var last_month = GetValue(standard_economy_module, "last_month")?.GetChild(1);
			if (last_month != null)
			{
				var energyIncome = GetValue(last_month, "energy");
				country.EnergyIncome = Convert.ToDouble(energyIncome.GetChild(1).GetText());

				var mineralsIncome = GetValue(last_month, "minerals");
				country.MineralsIncome = Convert.ToDouble(mineralsIncome.GetChild(1).GetText());

				var foodIncome = GetValue(last_month, "food");
				if (foodIncome != null)
					country.FoodIncome = Convert.ToDouble(foodIncome.GetChild(1).GetText());
				else
					country.FoodIncome = 0;


				var influenceIncome = GetValue(last_month, "influence");
				country.InfluenceIncome = Convert.ToDouble(influenceIncome.GetChild(1).GetText());

				var unityIncome = GetValue(last_month, "unity");
				country.UnityIncome = Convert.ToDouble(unityIncome.GetChild(1).GetText());

				var traditions = GetValue(kvPairs, "traditions");
				for (int j = 1; j < traditions?.ChildCount - 1; j++)
					country.Traditions.Add(traditions.GetChild(j).GetText().Trim('"'));

				var physics_research = GetValue(last_month, "physics_research");
				country.PhysicsResearchIncome = Convert.ToDouble(physics_research.GetChild(1).GetText());

				var society_research = GetValue(last_month, "society_research");
				country.SocietyResearchIncome = Convert.ToDouble(society_research.GetChild(1).GetText());

				var engineering_research = GetValue(last_month, "engineering_research");
				country.EngineeringResearchIncome = Convert.ToDouble(engineering_research.GetChild(1).GetText());
			}

			country.IsMachineEmpire = GetStringValue(kvPairs, "customization") == "machines";
		}

		/// <summary>
		/// 返回Planet ID
		/// </summary>
		/// <param name="owned_planets"></param>
		/// <returns></returns>
		private IEnumerable<string> GetColonies(IParseTree owned_planets)
		{
			for (int j = 1; j < owned_planets.ChildCount - 1; j++)
			{
				yield return owned_planets.GetChild(j).GetText();
			}
		}

		private Planet GetPlanet(string planetId)
		{
			var planetData = GetValue(planetsData, planetId).GetChild(1);
			var tileData = GetValue(planetData, "tiles").GetChild(1);
			var name = GetStringValue(planetData, "name");
			return new Planet() { Id = planetId, Name = name, Pops = GetPlanetPops(tileData, popData) };
		}

		private static int GetTechnologyCount(ParadoxParser.ParadoxContext kvPairs)
		{
			var tech_status = GetValue(kvPairs, "tech_status").GetChild(1);
			int levels = 0;
			for (int j = 0; j < tech_status.ChildCount; j++)
			{
				if (tech_status.GetChild(j).GetChild(0).GetText() == "level")
					levels += Convert.ToInt32(tech_status.GetChild(j).GetChild(2).GetText().Trim('"'));
			}

			return levels;
		}

		private List<Pop> GetPlanetPops(IParseTree tileData, IParseTree popData)
		{
			var pops = new List<Pop>();
			for (int i = 0; i < tileData.ChildCount; i++)
			{
				var popId = GetValue(tileData.GetChild(i).GetChild(2).GetChild(1), "pop")?.GetText();

				if (popId != null)
				{
					var pop = GetPop(popId, popData);
					if (pop != null)
						pops.Add(pop);
				}
			}

			return pops;
		}

		private Pop GetPop(string popId, IParseTree popData)
		{
			//当行星被轰炸时，可能人口死亡，行星数据里还有pop id，但pop数据库里已经没有了。
			var body = GetValue(popData, popId)?.GetChild(1);
			if (body == null)
				return null;

			var factionId = GetValue(body, "pop_faction")?.GetText();
			string factionName = null;
			if (factionId != null)
			{
				var faction = GetValue(pop_factionsData, factionId).GetChild(1);
				factionName = GetStringValue(faction, "name");
			}

			return new Pop { Id = popId, Faction = factionName };
		}

		public List<PlanetTiles> GetCountryPlanetTiles(string tag)
		{
			var countryData = GetValue(countriesData, tag);
			var scope = countryData as ParadoxParser.ScopeContext;
			if (scope == null)
				throw new ArgumentException($"{tag} is not a playable country.");

			var owned_planets = GetValue(scope.paradox(), "owned_planets");
			return (from planetId in GetColonies(owned_planets)
					select GetPlanetTitles(planetId)).ToList();
		}

		public PlanetTiles GetPlanetTitles(string planetId)
		{
			var planetData = GetValue(planetsData, planetId).GetChild(1);
			var planetSize = Convert.ToInt32(GetValue(planetData, "planet_size")?.GetText());

			var tiles = GetValue(planetData, "tiles").GetChild(1);
			var planetTiles = new PlanetTiles();
			planetTiles.Id = planetId;
			planetTiles.Name = GetStringValue(planetData, "name");
			planetTiles.Tiles = new Dictionary<int, Tile>(planetSize);
			for (int titleIndex = 0; titleIndex < planetSize; titleIndex++)
			{
				var tileData = tiles.GetChild(titleIndex);

				var tile = new Tile();
				planetTiles.Tiles[Convert.ToInt32(tileData.GetChild(0).GetText())] = tile;

				var resources = GetValue(tileData.GetChild(2).GetChild(1), "resources")?.GetChild(1);
				if (resources != null)
				{
					for (int i = 0; i < resources.ChildCount; i++)
					{
						var resource = resources.GetChild(i);
						string resourceName = resource.GetChild(0).GetText();
						var value = resource.GetChild(2).GetChild(1).GetText();
						Debug.Assert(resource.GetChild(2).GetChild(2).GetText() == value, $"planetId={planetId}, tileId={tileData.GetChild(0).GetText()}，一项资源的两个数值不同：\n{resource.GetText()}");

						//合并各种科技产出
						if (resourceName == "physics_research" || resourceName == "society_research" || resourceName == "engineering_research")
						{
							if (tile.Resources.ContainsKey("research"))
								tile.Resources["research"] += Convert.ToDouble(value);
							else
								tile.Resources["research"] = Convert.ToDouble(value);
						}
						else if (resourceName == "energy" || resourceName == "minerals" || resourceName == "food")
							tile.Resources[resourceName] = Convert.ToDouble(value);
						else
							tile.Resources["special"] = 1;
					}
				}
			}

			return planetTiles;
		}


		private static string GetMatchedScope(string content, string scopeName)
		{
			string lineEndingSymbol = content.DetectLineEnding();

			int bracketBalance = 0;
			var start = content.IndexOf(scopeName + "={");
			int i = start;
			for (; i < content.Length; i++)
			{
				if (content[i] == '{')
					bracketBalance++;
				else if (content[i] == '}')
				{
					bracketBalance--;
					if (bracketBalance == 0)
						return content.Substring(start, i - start + 1);
				}
				else if (content[i] == '"')
				{
					i = content.IndexOf('"', i + 1);
					if (i == -1)
						break;
				}
				else if (content[i] == '#')
				{
					i = content.IndexOf(lineEndingSymbol, i + 1);
					if (i == -1)
						break;
				}
			}

			return null;
		}

		public static async Task<IParseTree> GetScopeBodyAsync(string input)
		{
			//ICharStream cstream = CharStreams.fromStream(stream);


			var d = await Task.Run(() => GetScopeBody(input));
			return d;
		}

		private static IParseTree GetScopeBody(string input)
		{
			ICharStream cstream = CharStreams.fromstring(input);
			ITokenSource lexer = new ParadoxLexer(cstream);
			ITokenStream tokens = new CommonTokenStream(lexer);
			var parser = new ParadoxParser(tokens);

			var data = parser.kvPair().children;
			return data[2].GetChild(1);
		}

		/// <summary>
		/// 如果找不到则返回null。
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IParseTree GetValue(IParseTree tree, string key)
		{
			for (int i = 0; i < tree.ChildCount; i++)
			{
				if (tree.GetChild(i).GetChild(0).GetText() == key)
					return tree.GetChild(i).GetChild(2);
			}
			return null;
		}

		/// <summary>
		/// 如果找不到则返回null。
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string GetStringValue(IParseTree tree, string key)
		{
			return GetValue(tree, key)?.GetText().Trim('"');
		}
	}
}