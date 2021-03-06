﻿using DarkSkyApi.Models;
using Feels.Models;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace Feels.Services {
    public class TileDesigner {
        public static async void UpdatePrimary(BasicGeoposition position) {
            var data = App.DataSource;
            if (data == null) return;

            var tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            tileUpdater.Clear();
            tileUpdater.EnableNotificationQueue(true);

            var town = await GetLocation(position);

            tileUpdater.Update(CreateTileCurrent(data.Forecast, town));
            tileUpdater.Update(CreateTileCurrentDetails(data.Forecast));
            tileUpdater.Update(CreateTileHourly(data.Forecast.Hourly));
            tileUpdater.Update(CreateTileDaily(data.Forecast.Daily));
        }

        public static void UpdateSecondary(string tileId, 
            Forecast forecast, LocationItem location) {

            if (forecast == null) return;

            var tileUpdater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tileId);
            tileUpdater.Clear();
            tileUpdater.EnableNotificationQueue(true);

            var town = location.Name;

            tileUpdater.Update(CreateTileCurrent(forecast, town));
            tileUpdater.Update(CreateTileCurrentDetails(forecast));
            tileUpdater.Update(CreateTileHourly(forecast.Hourly));
            tileUpdater.Update(CreateTileDaily(forecast.Daily));
        }

        public static async Task<bool> PinSecondaryTile(LocationItem location) {
            var id = ConvertLocationNameToTileId(location.Name);
            var displayName = location.Name;
            var activationArguments = id;
            var uriSquare150x150Logo = new Uri("ms-appx:///Assets/Square150x150Logo.scale-100.png");
            var desiredSize = TileSize.Square150x150;
            
            var tile = new SecondaryTile(id, displayName, activationArguments, uriSquare150x150Logo, desiredSize);

            tile.VisualElements.ShowNameOnSquare150x150Logo = false;
            tile.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/LargeTile.scale-100.png");
            tile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/Wide310x150Logo.scale-100.png");

            var isPineed =  await tile.RequestCreateAsync();

            if (!isPineed) return false;
            return true;
        }

        public static async Task<bool> UnpinSecondaryTile(LocationItem location) {
            var id = ConvertLocationNameToTileId(location.Name);
            return await UnpinSecondaryTile(id);
        }

        public static async Task<bool> UnpinSecondaryTile(string locationId) {
            var tile = new SecondaryTile(locationId);
            return await tile.RequestDeleteAsync();
        }

        public static string ConvertLocationNameToTileId(string locationName) {
            return locationName.Replace(" ", "").Replace(",", ".");
        }

        public static string ConvertTileIdToLocationName(string tileId) {
            return tileId.Replace(".", ", ");
        }

        public static bool IsSecondaryTilePinned(LocationItem location) {
            return IsSecondaryTileExist(location.Name);
        }

        public static bool IsSecondaryTileExist(string locationName) {
            var tileId = ConvertLocationNameToTileId(locationName);
            return SecondaryTile.Exists(tileId);
        }

        // ------------------
        // EXTRACTION METHODS
        // ------------------
        private static async Task<string> GetLocation(BasicGeoposition position) {
            Geopoint pointToReverseGeocode = new Geopoint(position);

            // Reverse geocode the specified geographic location.
            MapLocationFinderResult result =
                await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

            // If the query returns results, display the name of the town
            // contained in the address of the first result.
            if (result.Status == MapLocationFinderStatus.Success && result.Locations.Count != 0) {
                return result.Locations[0].Address.Town;
            }

            return "";
        }

        private static string GetTemperature(float temperature) {
            return ((int)temperature).ToString() + "°";
        }

        private static string GetPrecipProbability(float probability) {
            if (probability < 0.05) return "";
            return (probability * 100).ToString() + "%";
        }

        private static string GetIcon(string condition) {
            var path = "";

            switch (condition) {
                case "clear-day":
                    path = "Assets/TileIcons/sun.png";
                    break;
                case "clear-night":
                    path = "Assets/TileIcons/moon.png";
                    break;
                case "partly-cloudy-day":
                    path = "Assets/TileIcons/partycloudy_day.png";
                    break;
                case "partly-cloudy-night":
                    path = "Assets/TileIcons/partycloudy_night.png";
                    break;
                case "cloudy":
                    path = "Assets/TileIcons/cloudy.png";
                    break;
                case "rain":
                    path = "Assets/TileIcons/rain.png";
                    break;
                case "sleet": // neige fondu
                    path = "Assets/TileIcons/sleet.png";
                    break;
                case "snow":
                    path = "Assets/TileIcons/snow.png";
                    break;
                case "wind":
                    path = "Assets/TileIcons/wind.png";
                    break;
                case "fog":
                    path = "Assets/TileIcons/fog.png";
                    break;
                default:
                    path = "Assets/TileIcons/sun.png";
                    break;
            }

            return path;
        }

        private static string GetIcon(Forecast forecast) {
            var currentForecast = forecast.Currently;
            var condition = currentForecast.Icon;

            switch (condition) {
                case "clear-night":
                case "partly-cloudy-night":
                    return GetMoonPhaseIcon(forecast.Daily.Days[0]);
                default:
                    return GetIcon(condition);
            }
        }

        private static string GetMoonPhaseIcon(DayDataPoint todayForecast) {
            var moonPhase = todayForecast.MoonPhase;
            string path = null;

            if (moonPhase == 0) {
                path = "ms-appx:///Assets/TileIcons/moon_new.png";

            } else if (moonPhase > 0 && moonPhase < .25) {
                path = "ms-appx:///Assets/TileIcons/moon_waxing_crescent.png";

            } else if (moonPhase == .25) {
                path = "ms-appx:///Assets/TileIcons/moon_first_quarter.png";

            } else if (moonPhase > .25 && moonPhase < .5) {
                path = "ms-appx:///Assets/TileIcons/moon_waxing_gibbous.png";

            } else if (moonPhase == .5) {
                path = "ms-appx:///Assets/TileIcons/moon_full.png";

            } else if (moonPhase > .5 && moonPhase < .75) {
                path = "ms-appx:///Assets/TileIcons/moon_waning_gibbous.png";

            } else if (moonPhase == .75) {
                path = "ms-appx:///Assets/TileIcons/moon_third_quarter.png";

            } else { // moonPhase > .75
                path = "ms-appx:///Assets/TileIcons/moon_waning_crescent.png";
            }

            return path;
        }

        private static string GetPrecipIcon(string condition) {
            var path = "";

            switch (condition) {
                case "rain":
                    path = "Assets/Icons/rain.png";
                    break;
                case "sleet": // neige fondu
                    path = "Assets/Icons/sleet.png";
                    break;
                case "snow":
                    path = "Assets/Icons/snow.png";
                    break;
                default:
                    path = "";
                    break;
            }

            return path;
        }

        private static string GetWindIcon(float speed) {
            var path = "";

            switch (speed) {
                default:
                    path = "Assets/TileIcons/wind_direction.png";
                    break;
            }

            return path;
        }

        // ---------------
        // TILES CREATIONS
        // ---------------
        private static TileNotification CreateTileCurrent(Forecast forecast, string town) {
            var currentForecast = forecast.Currently;
            var condition = currentForecast.Icon;
            var currentTemperature = GetTemperature(forecast.Currently.ApparentTemperature);
            var maxTemperature = GetTemperature(forecast.Daily.Days[0].ApparentMaxTemperature);
            var minTemperature = GetTemperature(forecast.Daily.Days[0].ApparentMinTemperature);

            var time = DateTime.Now.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);

            // Generate Visual
            var content = new TileContent() {
                Visual = new TileVisual() {
                    LockDetailedStatus1 = GetDetailedStatus(),
                    TileSmall = GetSmallVisual(),
                    TileMedium = GetMediumVisual(),
                    TileWide = GetWideVisual(),
                    TileLarge = GetLargeVisual()
                }
            };

            string GetDetailedStatus()
            {
                string formatedText = string.Format("{0} {1} ({2}/{3}) {4}", 
                    town, currentTemperature, minTemperature, maxTemperature, forecast.Currently.Summary);
                return formatedText;
            }

            TileBinding GetSmallVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveText() {
                                Text = currentTemperature,
                                HintAlign = AdaptiveTextAlign.Center,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            },
                        }
                    }
                };
            }

            TileBinding GetMediumVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveText() {
                                Text = town.ToUpper(),
                                HintAlign = AdaptiveTextAlign.Center,
                                HintStyle = AdaptiveTextStyle.Body
                            },
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() { HintWeight = 1 },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 2,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = GetIcon(forecast),
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() { HintWeight = 1 }
                                }
                            },
                            new AdaptiveGroup() {
                                Children = {
                                    // Temperature
                                    new AdaptiveSubgroup() {
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = currentTemperature,
                                                HintAlign = AdaptiveTextAlign.Center,
                                                HintStyle = AdaptiveTextStyle.Subtitle
                                            },
                                        }
                                    },
                                    // Time
                                    new AdaptiveSubgroup() {
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = time.ToString(),
                                                HintAlign = AdaptiveTextAlign.Left,
                                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                                           }
                                        }
                                    }                                    
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetWideVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        TextStacking = TileTextStacking.Center,
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() {HintWeight = 5},
                                    new AdaptiveSubgroup() {
                                        HintWeight = 25,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = GetIcon(forecast),
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 30,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = currentTemperature,
                                                HintStyle = AdaptiveTextStyle.Title
                                            },
                                            new AdaptiveText() {
                                                Text = time,
                                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 40,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = forecast.Currently.Summary,
                                                HintStyle = AdaptiveTextStyle.Body
                                            },
                                            new AdaptiveText() {
                                                Text = town,
                                                HintStyle = AdaptiveTextStyle.BaseSubtle
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetLargeVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        TextStacking = TileTextStacking.Center,
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() {HintWeight = 5},
                                    new AdaptiveSubgroup() {
                                        HintWeight = 25,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = GetIcon(forecast),
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 30,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = currentTemperature,
                                                HintStyle = AdaptiveTextStyle.Title
                                            },
                                            new AdaptiveText() {
                                                Text = time,
                                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 40,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = forecast.Currently.Summary,
                                                HintStyle = AdaptiveTextStyle.Body
                                            },
                                            new AdaptiveText() {
                                                Text = town,
                                                HintStyle = AdaptiveTextStyle.BaseSubtle
                                            }
                                        }
                                    }
                                }
                            },

                            new AdaptiveText(), // for spacing

                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() {HintWeight = 10}, // spacing
                                    // Max - min temperatures
                                    new AdaptiveSubgroup() {
                                        HintWeight = 30,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = maxTemperature,
                                                HintStyle = AdaptiveTextStyle.Subtitle
                                            },
                                            new AdaptiveText() {
                                                Text = minTemperature,
                                                HintStyle = AdaptiveTextStyle.SubtitleSubtle
                                            }
                                        }
                                    },

                                    // Wind & Precip icons
                                    new AdaptiveSubgroup() {
                                        HintWeight = 15,
                                        Children = {
                                            //new AdaptiveText(),
                                            new AdaptiveImage() {
                                                Source = GetWindIcon(forecast.Currently.WindSpeed),
                                                HintRemoveMargin = true
                                            },

                                            new AdaptiveText(), // spacing

                                            new AdaptiveImage() {
                                                Source = GetPrecipIcon(forecast.Currently.PrecipitationType),
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },

                                    new AdaptiveSubgroup() {HintWeight = 3}, // spacing

                                    // Wind & Precip values
                                    new AdaptiveSubgroup() {
                                        HintWeight = 40,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = forecast.Currently.WindSpeed.ToString(),
                                                HintStyle = AdaptiveTextStyle.Subtitle
                                            },

                                            new AdaptiveText(),
                                            new AdaptiveText() {
                                                Text = GetPrecipProbability(forecast.Currently.PrecipitationProbability),
                                                HintStyle = AdaptiveTextStyle.Subtitle
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            return new TileNotification(content.GetXml());
        }

        static TileNotification CreateTileCurrentDetails(Forecast forecast) {
            var content = new TileContent() {
                Visual = new TileVisual() {
                    TileMedium = GetMediumVisual(),
                    TileWide = GetWideVisual()
                }
            };

            TileBinding GetMediumVisual() {
                //var precipIconPath = GetIcon(forecast.Currently.Icon);
                //var precipProba = (forecast.Currently.PrecipitationProbability * 100).ToString();
                var maxTemp = ((int)forecast.Daily.Days[0].MaxTemperature).ToString() + "°";
                var minTemp = ((int)forecast.Daily.Days[0].MinTemperature).ToString() + "°";

                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            // Max temp
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() { HintWeight = 1 },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 2,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = maxTemp,
                                                HintStyle = AdaptiveTextStyle.Title
                                            }
                                        }
                                    }
                                }
                            },

                            // Min temp
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() { HintWeight = 1 },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 2,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = minTemp,
                                                HintStyle = AdaptiveTextStyle.TitleSubtle
                                            }
                                        }
                                    }
                                }
                            },

                            // Wind direction
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() { HintWeight = 1 },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 1,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = "Assets/TileIcons/wind_direction.png",
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 2,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = forecast.Currently.WindSpeed.ToString(),
                                                HintStyle = AdaptiveTextStyle.Caption
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetWideVisual() {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    // Precip proba
                                    new AdaptiveSubgroup() {
                                        HintWeight = 1,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = "Assets/TileIcons/precip_proba.png",
                                                HintRemoveMargin = true
                                            },
                                            new AdaptiveText() {
                                                Text = string.Format("{0}%", forecast.Currently.PrecipitationProbability * 100),
                                                HintStyle = AdaptiveTextStyle.Caption,
                                                HintAlign = AdaptiveTextAlign.Center
                                            }
                                        }
                                    },

                                    // Humidity
                                    new AdaptiveSubgroup() {
                                        HintWeight = 1,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = "Assets/TileIcons/humidity.png",
                                                HintRemoveMargin = true
                                            },
                                            new AdaptiveText() {
                                                Text = string.Format("{0}%", forecast.Currently.Humidity * 100),
                                                HintStyle = AdaptiveTextStyle.Caption,
                                                HintAlign = AdaptiveTextAlign.Center
                                            }
                                        }
                                    },

                                    // Cloud cover
                                    new AdaptiveSubgroup() {
                                        HintWeight = 1,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = "Assets/TileIcons/cloudy.png",
                                                HintRemoveMargin = true
                                            },
                                            new AdaptiveText() {
                                                Text = string.Format("{0}%", forecast.Currently.CloudCover * 100),
                                                HintStyle = AdaptiveTextStyle.Caption,
                                                HintAlign = AdaptiveTextAlign.Center
                                            }
                                        }
                                    },

                                    // Wind speed
                                    new AdaptiveSubgroup() {
                                        HintWeight = 1,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = "Assets/TileIcons/wind.png",
                                                HintRemoveMargin = true
                                            },
                                            new AdaptiveText() {
                                                Text = string.Format("{0}{1}", forecast.Currently.WindSpeed, GetWindSpeedUnits()),
                                                HintStyle = AdaptiveTextStyle.Caption,
                                                HintAlign = AdaptiveTextAlign.Center
                                            }
                                        }
                                    },

                                    // Wind direction
                                    new AdaptiveSubgroup() {
                                        HintWeight = 1,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = "Assets/TileIcons/wind_direction.png",
                                                HintRemoveMargin = true
                                            },
                                            new AdaptiveText() {
                                                Text = string.Format("{0}°", forecast.Currently.WindBearing),
                                                HintStyle = AdaptiveTextStyle.Caption,
                                                HintAlign = AdaptiveTextAlign.Center
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            return new TileNotification(content.GetXml());
        }

        static TileNotification CreateTileHourly(HourlyForecast hourlyForecast) {
            var content = new TileContent() {
                Visual = new TileVisual() {
                    TileMedium = GetMediumVisual(),
                    TileWide = GetWideVisual(),
                    TileLarge = GetLargeVisual()
                }
            };

            TileBinding GetMediumVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateHourlySubGroup(hourlyForecast.Hours[1]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[2])
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetWideVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateHourlySubGroup(hourlyForecast.Hours[1]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[2]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[3]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[4]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[5]),
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetLargeVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        TextStacking = TileTextStacking.Center,
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateHourlySubGroup(hourlyForecast.Hours[1]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[2]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[3]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[4]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[5]),
                                }
                            },
                            new AdaptiveText(), // for spacing
                            new AdaptiveGroup() {
                                Children = {
                                    CreateHourlySubGroup(hourlyForecast.Hours[6]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[7]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[8]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[9]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[10]),
                                }
                            }
                        }
                    }
                };
            }

            return new TileNotification(content.GetXml());
        }

        static TileNotification CreateTileDaily(DailyForecast dailyForecast) {
            var content = new TileContent() {
                Visual = new TileVisual() {
                    TileMedium = GetMediumVisual(),
                    TileWide = GetWideVisual(),
                    TileLarge = GetLargeVisual()
                }
            };

            TileBinding GetMediumVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateDailySubGroup(dailyForecast.Days[1]),
                                    CreateDailySubGroup(dailyForecast.Days[2])
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetWideVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateDailySubGroup(dailyForecast.Days[1]),
                                    CreateDailySubGroup(dailyForecast.Days[2]),
                                    CreateDailySubGroup(dailyForecast.Days[3]),
                                    CreateDailySubGroup(dailyForecast.Days[4]),
                                    CreateDailySubGroup(dailyForecast.Days[5])
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetLargeVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateDailySubGroup(dailyForecast.Days[1]),
                                    CreateDailySubGroup(dailyForecast.Days[2]),
                                    CreateDailySubGroup(dailyForecast.Days[3]),
                                    CreateDailySubGroup(dailyForecast.Days[4]),                                    
                                }
                            },
                            //new AdaptiveText(), // for spacing
                            new AdaptiveGroup() {
                                Children = {
                                    CreateDailySubGroup(dailyForecast.Days[5]),
                                    CreateDailySubGroup(dailyForecast.Days[6]),
                                    CreateDailySubGroup(dailyForecast.Days[7])
                                }
                            }
                        }
                    }
                };
            }

            return new TileNotification(content.GetXml());
        }

        private static AdaptiveSubgroup CreateHourlySubGroup(HourDataPoint hour) {
            return new AdaptiveSubgroup() {
                HintWeight = 1,
                Children = {
                    new AdaptiveText() {
                        Text = hour.Time.ToLocalTime().ToString("htt", CultureInfo.InvariantCulture),
                        HintAlign = AdaptiveTextAlign.Center,
                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                    },
                    new AdaptiveImage() {
                        Source = GetIcon(hour.Icon),
                        HintRemoveMargin = true
                    },
                    new AdaptiveText() {
                        Text = ((int)hour.ApparentTemperature).ToString() + "°",
                        HintStyle = AdaptiveTextStyle.Base,
                        HintAlign = AdaptiveTextAlign.Center
                    }
                }
            };
        }

        private static AdaptiveSubgroup CreateDailySubGroup(DayDataPoint day) {
            return new AdaptiveSubgroup() {
                HintWeight = 1,
                Children = {
                    new AdaptiveText() {
                        Text = day.Time.ToString("ddd"),
                        HintAlign = AdaptiveTextAlign.Center,
                        HintStyle = AdaptiveTextStyle.Caption
                    },
                    new AdaptiveImage() {
                        Source = GetIcon(day.Icon),
                        HintRemoveMargin = true
                    },
                    new AdaptiveText() {
                        Text = ((int)day.ApparentMaxTemperature).ToString() + "°",
                        HintAlign = AdaptiveTextAlign.Center,
                        HintStyle = AdaptiveTextStyle.Caption
                    },
                    new AdaptiveText() {
                        Text = ((int)day.ApparentMinTemperature).ToString() + "°",
                        HintAlign = AdaptiveTextAlign.Center,
                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                    }
                }
            };
        }

        private static string GetWindSpeedUnits() {
            var unit = Settings.GetUnit();

            switch (unit) {
                case DarkSkyApi.Unit.US:
                    return "miles/h";
                case DarkSkyApi.Unit.SI:
                    return "m/s";
                case DarkSkyApi.Unit.CA:
                    return "km/h";
                case DarkSkyApi.Unit.UK:
                    return "miles/h";
                case DarkSkyApi.Unit.UK2:
                    return "miles/h";
                case DarkSkyApi.Unit.Auto:
                    return "m/s";
                default:
                    return "miles/h";
            }
        }
    }
}
