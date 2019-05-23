﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FandomParser.Core;
using InfoboxParser.Models;

namespace InfoboxParser
{
    public class InfoboxParser
    {
        //|Input 1 Amount = 2
        private static readonly Regex regexInputAmount = new Regex(@"(?<begin>\|Input)\s*(?<counter>\d+)\s*(?<end>Amount)\s*(?<equalSign>[=])\s*(?<value>\d*(?:[\.\,]\d*)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //|Input 1 Amount Electricity = 4
        private static readonly Regex regexInputAmountElectricity = new Regex(@"(?<begin>\|Input)\s*(?<counter>\d+)\s*(?<end>Amount Electricity)\s*(?<equalSign>[=])\s*(?<value>\d*(?:[\.\,]\d*)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //|Input 1 Icon = Potato.png
        private static readonly Regex regexInputIcon = new Regex(@"(?<begin>\|Input)\s*(?<counter>\d+)\s*(?<end>Icon)\s*(?<equalSign>[=])\s*(?<fileName>(?:\w*\s*)+(?:[\.]\w*)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ICommons _commons;

        public InfoboxParser(ICommons commons)
        {
            _commons = commons;
        }

        public IInfobox GetInfobox(string wikiText)
        {
            if (string.IsNullOrWhiteSpace(wikiText))
            {
                return null;
            }

            IInfobox result = new Infobox();

            var buildingName = getBuildingName(wikiText);
            if (string.IsNullOrWhiteSpace(buildingName))
            {
            }

            var buildingType = getBuildingType(wikiText);
            if (buildingType == BuildingType.Unknown)
            {

            }

            ProductionInfo productionInfo = null;
            //TODO parse infoboxes containing information of multiple worlds
            if (!wikiText.StartsWith(_commons.InfoboxTemplateStartBothWorlds))
            {
                productionInfo = getProductionInfo(wikiText);
                if (productionInfo == null && buildingType == BuildingType.Production)
                {

                }
            }

            //handle special cases
            switch (buildingName)
            {
                case "Bombin Weaver":
                    buildingName = "Bomb­ín Weaver";
                    break;
                default:
                    break;
            }

            result.Name = buildingName;
            result.Type = buildingType;
            result.ProductionInfos = productionInfo;

            return result;
        }

        private BuildingType getBuildingType(string infobox)
        {
            var result = BuildingType.Unknown;

            //short circuit infoboxes without building type info
            if (!infobox.Contains("|Building Type"))
            {
                return result;
            }

            using (var reader = new StringReader(infobox))
            {
                string curLine;
                while ((curLine = reader.ReadLine()) != null)
                {
                    curLine = curLine.Replace(_commons.InfoboxTemplateEnd, string.Empty);

                    if (curLine.StartsWith("|Building Type", StringComparison.OrdinalIgnoreCase))
                    {
                        var buildingType = curLine.Replace("|Building Type (OW)", string.Empty)
                            .Replace("|Building Type (NW)", string.Empty)
                            .Replace("|Building Type", string.Empty)
                            .Replace("=", string.Empty)
                            .Replace(" ", string.Empty)
                            .Trim();

                        if (Enum.TryParse(buildingType, ignoreCase: true, out BuildingType parsedBuildingType))
                        {
                            result = parsedBuildingType;
                        }

                        break;
                    }
                }
            }

            return result;
        }

        private string getBuildingName(string infobox)
        {
            var result = string.Empty;

            if (!infobox.Contains("|Title"))
            {
                return result;
            }

            using (var reader = new StringReader(infobox))
            {
                string curLine;
                while ((curLine = reader.ReadLine()) != null)
                {
                    curLine = curLine.Replace(_commons.InfoboxTemplateEnd, string.Empty);

                    if (curLine.StartsWith("|Title", StringComparison.OrdinalIgnoreCase))
                    {
                        result = curLine.Replace("|Title", string.Empty)
                            .Replace("=", string.Empty)
                            .Trim();
                        break;
                    }
                }
            }

            return result;
        }

        private ProductionInfo getProductionInfo(string infobox)
        {
            ProductionInfo result = null;

            //short circuit infoboxes without production info
            if (!infobox.Contains("|Produces Amount"))
            {
                return result;
            }

            result = new ProductionInfo();
            result.EndProduct = new EndProduct();

            using (var reader = new StringReader(infobox))
            {
                string curLine;
                while ((curLine = reader.ReadLine()) != null)
                {
                    curLine = curLine.Replace(_commons.InfoboxTemplateEnd, string.Empty);

                    if (curLine.StartsWith("|Produces Amount Electricity", StringComparison.OrdinalIgnoreCase))
                    {
                        var productionAmountElectricity = curLine.Replace("|Produces Amount Electricity", string.Empty)
                            .Replace("=", string.Empty)
                            .Trim();

                        if (double.TryParse(productionAmountElectricity, out double parsedProductionAmountElectricity))
                        {
                            result.EndProduct.AmountElectricity = parsedProductionAmountElectricity;
                        }
                    }
                    else if (curLine.StartsWith("|Produces Amount", StringComparison.OrdinalIgnoreCase))
                    {
                        var productionAmount = curLine.Replace("|Produces Amount", string.Empty)
                            .Replace("=", string.Empty)
                            .Trim();

                        if (double.TryParse(productionAmount, out double parsedProductionAmount))
                        {
                            result.EndProduct.Amount = parsedProductionAmount;
                        }
                    }
                    else if (curLine.StartsWith("|Produces Icon", StringComparison.OrdinalIgnoreCase))
                    {
                        var icon = curLine.Replace("|Produces Icon", string.Empty)
                            .Replace("=", string.Empty)
                            .Trim();

                        result.EndProduct.Icon = icon;
                    }
                    else if (curLine.StartsWith("|Input ", StringComparison.OrdinalIgnoreCase))
                    {
                        var matchAmount = regexInputAmount.Match(curLine);
                        if (matchAmount.Success)
                        {
                            var matchedCounter = matchAmount.Groups["counter"].Value;
                            if (!int.TryParse(matchedCounter, out int counter))
                            {
                                throw new Exception("could not find counter");
                            }

                            //handle entry with no value e.g. "|Input 2 Amount     = "
                            var matchedValue = matchAmount.Groups["value"].Value.Replace(",", ".");
                            if (string.IsNullOrWhiteSpace(matchedValue))
                            {
                                continue;
                            }

                            if (!double.TryParse(matchedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double inputValue))
                            {
                                throw new Exception("could not find value for input");
                            }

                            var foundInputProduct = result.InputProducts.FirstOrDefault(x => x.Order == counter);
                            if (foundInputProduct == null)
                            {
                                foundInputProduct = new InputProduct
                                {
                                    Order = counter
                                };

                                result.InputProducts.Add(foundInputProduct);
                            }

                            foundInputProduct.Amount = inputValue;

                            continue;
                        }

                        var matchAmountElectricity = regexInputAmountElectricity.Match(curLine);
                        if (matchAmountElectricity.Success)
                        {
                            var matchedCounter = matchAmountElectricity.Groups["counter"].Value;
                            if (!int.TryParse(matchedCounter, out int counter))
                            {
                                throw new Exception("could not find counter");
                            }

                            //handle entry with no value e.g. "|Input 1 Amount Electricity    = "
                            var matchedValue = matchAmountElectricity.Groups["value"].Value.Replace(",", ".");
                            if (string.IsNullOrWhiteSpace(matchedValue))
                            {
                                continue;
                            }

                            if (!double.TryParse(matchedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double inputValue))
                            {
                                throw new Exception("could not find value for input");
                            }

                            var foundInputProduct = result.InputProducts.FirstOrDefault(x => x.Order == counter);
                            if (foundInputProduct == null)
                            {
                                foundInputProduct = new InputProduct
                                {
                                    Order = counter
                                };

                                result.InputProducts.Add(foundInputProduct);
                            }

                            foundInputProduct.AmountElectricity = inputValue;

                            continue;
                        }

                        var matchIcon = regexInputIcon.Match(curLine);
                        if (matchIcon.Success)
                        {
                            var matchedCounter = matchIcon.Groups["counter"].Value;
                            if (!int.TryParse(matchedCounter, out int counter))
                            {
                                throw new Exception("could not find counter");
                            }

                            //handle entry with no value e.g. "|Input 1 Icon = "
                            var matchedFileName = matchIcon.Groups["fileName"].Value;
                            if (string.IsNullOrWhiteSpace(matchedFileName))
                            {
                                continue;
                            }

                            var foundInputProduct = result.InputProducts.FirstOrDefault(x => x.Order == counter);
                            if (foundInputProduct == null)
                            {
                                foundInputProduct = new InputProduct
                                {
                                    Order = counter
                                };

                                result.InputProducts.Add(foundInputProduct);
                            }

                            foundInputProduct.Icon = matchedFileName;

                            continue;
                        }
                    }
                }
            }

            return result;
        }
    }
}
