﻿#region Copyright (C) 2011 MPExtended
// Copyright (C) 2011 MPExtended Developers, http://mpextended.codeplex.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using MPExtended.Libraries.ServiceLib;
using MPExtended.Services.MediaAccessService.Interfaces;
using MPExtended.Libraries.ServiceLib.DB;
using MPExtended.Services.MediaAccessService.Interfaces.TVShow;

namespace MPExtended.PlugIns.MAS.MPTVSeries
{
    internal class MPTvSeriesDB : Database
    {
        public MPTvSeriesDB()
            : base(Configuration.GetMPDbLocations().TvSeries)
        {
        }

        #region Series
        public int GetSeriesCount()
        {
            return GetSeries(null, null).Count;
        }

        public List<WebSeries> GetAllSeries()
        {
            return GetSeries(null, null);
        }

        public List<WebSeries> GetSeries(int? start, int? end)
        {
            // Please contact me if you've a better way to do this, this just sucks. I could use a GROUP BY but SQLite gets really slow in the long version
            // below, plus i'd have to use group_concat and string splitting, which also sucks.
            string sql = "SELECT DISTINCT series.ID, series.Pretty_Name, series.EpisodeCount, series.IMDB_ID, series.Rating, series.RatingCount, " +
                            "series.fanart, series.PosterBannerFileName, series.CurrentBannerFileName, series.Genre, local.Parsed_Name " +
                         "FROM online_series AS series " +
                         "INNER JOIN local_series AS local ON series.ID = local.ID AND local.Hidden = 0 " + 
                         "WHERE series.ID != 0 AND series.HasLocalFiles = 1";
            List<int> alreadyDoneList = new List<int>();
            return ReadList<WebSeries>(sql, delegate(SQLiteDataReader reader)
            {
                // we might have duplicate results due to the local.Parsed_Name thing
                WebSeries series = new WebSeries();
                series.Id = DatabaseHelperMethods.SafeInt32(reader, 0);
                if (alreadyDoneList.Contains(series.Id))
                    return null;
                alreadyDoneList.Add(series.Id);

                // MPTvSeries does some magic with the name: if it's empty in the online series, use the Parsed_Name from the local series. I prefer
                // a complete database, but we can't fix that easily. See DB Classes/DBSeries.cs:359 in MPTvSeries source
                series.PrettyName = DatabaseHelperMethods.SafeStr(reader, 1);
                if (String.IsNullOrEmpty(series.PrettyName))
                    series.PrettyName = DatabaseHelperMethods.SafeStr(reader, 10).Split('|').FirstOrDefault(); // let's hope noone adds a pipe to his series' name

                series.EpisodeCount = DatabaseHelperMethods.SafeInt32(reader, 2);
                series.ImdbId = DatabaseHelperMethods.SafeStr(reader, 3);
                series.Rating = DatabaseHelperMethods.SafeFloat(reader, 4);
                series.RatingCount = DatabaseHelperMethods.SafeInt32(reader, 5);

                String fanartUrl = DatabaseHelperMethods.SafeStr(reader, 6);
                series.CurrentFanartUrl = CreateFanartUrl(fanartUrl);

                String posterUrl = DatabaseHelperMethods.SafeStr(reader, 7);
                series.CurrentPosterUrl = CreateBannerUrl(posterUrl);

                String bannerUrl = DatabaseHelperMethods.SafeStr(reader, 8);
                series.CurrentBannerUrl = CreateBannerUrl(bannerUrl);
                series.GenreString = DatabaseHelperMethods.SafeStr(reader, 9);
                series.Genres = Utils.SplitString(series.GenreString);

                return series;
            }, start, end);
        }

        public WebSeriesFull GetFullSeries(int _seriesId)
        {
            string sql =
                  "SELECT DISTINCT series.ID, series.Pretty_Name, series.EpisodeCount, series.IMDB_ID, series.Rating, series.RatingCount, " +
                    "series.fanart, series.PosterBannerFileName, series.CurrentBannerFileName, series.Genre, " +
                    "series.origName, series.Status, series.SortName, series.BannerFileNames, series.Actors,series.PosterFileNames, " +
                    "series.ContentRating, series.Network, series.Summary, series.AirsDay, series.AirsTime, " +
                    "series.EpisodesUnWatched, series.Runtime, series.FirstAired, series.choosenOrder, local.Parsed_Name " +
                  "FROM online_series AS series " +
                  "INNER JOIN local_series AS local ON series.ID = local.ID AND local.Hidden = 0 " + 
                  "WHERE series.HasLocalFiles = 1 AND series.ID = " + _seriesId;
            return ReadRow<WebSeriesFull>(sql, delegate(SQLiteDataReader reader)
            {
                WebSeriesFull series = new WebSeriesFull();
                series.Id = DatabaseHelperMethods.SafeInt32(reader, 0);

                // MPTvSeries does some magic with the name: if it's empty in the online series, use the Parsed_Name from the local series. I prefer
                // a complete database, but we can't fix that easily. See DB Classes/DBSeries.cs:359 in MPTvSeries source
                series.PrettyName = DatabaseHelperMethods.SafeStr(reader, 1);
                if (String.IsNullOrEmpty(series.PrettyName))
                    series.PrettyName = DatabaseHelperMethods.SafeStr(reader, 25).Split('|').FirstOrDefault();

                series.EpisodeCount = DatabaseHelperMethods.SafeInt32(reader, 2);
                series.ImdbId = DatabaseHelperMethods.SafeStr(reader, 3);
                series.Rating = DatabaseHelperMethods.SafeFloat(reader, 4);
                series.RatingCount = DatabaseHelperMethods.SafeInt32(reader, 5);

                String fanartUrl = DatabaseHelperMethods.SafeStr(reader, 6);
                series.CurrentFanartUrl = CreateFanartUrl(fanartUrl);

                String posterUrl = DatabaseHelperMethods.SafeStr(reader, 7);
                series.CurrentPosterUrl = CreateBannerUrl(posterUrl);

                String bannerUrl = DatabaseHelperMethods.SafeStr(reader, 8);
                series.CurrentBannerUrl = CreateBannerUrl(bannerUrl);

                series.GenreString = DatabaseHelperMethods.SafeStr(reader, 9);
                series.Genres = Utils.SplitString(series.GenreString);
                series.OrigName = DatabaseHelperMethods.SafeStr(reader, 10);
                series.Status = DatabaseHelperMethods.SafeStr(reader, 11);
                series.SortName = DatabaseHelperMethods.SafeStr(reader, 12);

                String[] altBannerUrls = Utils.SplitString(DatabaseHelperMethods.SafeStr(reader, 13));
                for (int i = 0; i < altBannerUrls.Length; i++)
                {
                    altBannerUrls[i] = CreateBannerUrl(altBannerUrls[i]);
                }
                series.BannerUrls = altBannerUrls;

                series.Actors = Utils.SplitString(DatabaseHelperMethods.SafeStr(reader, 14));

                String[] altPosterUrls = Utils.SplitString(DatabaseHelperMethods.SafeStr(reader, 15));
                for (int i = 0; i < altPosterUrls.Length; i++)
                {
                    altPosterUrls[i] = CreateBannerUrl(altPosterUrls[i]);
                }
                series.PosterUrls = altPosterUrls;

                series.ContentRating = DatabaseHelperMethods.SafeStr(reader, 16);
                series.Network = DatabaseHelperMethods.SafeStr(reader, 17);
                series.Summary = DatabaseHelperMethods.SafeStr(reader, 18);
                series.AirsDay = DatabaseHelperMethods.SafeStr(reader, 19);
                series.AirsTime = DatabaseHelperMethods.SafeStr(reader, 20);
                series.EpisodesUnwatchedCount = DatabaseHelperMethods.SafeInt32(reader, 21);
                series.Runtime = DatabaseHelperMethods.SafeInt32(reader, 22);
                series.FirstAired = DatabaseHelperMethods.SafeDateTime(reader, 23);
                series.EpisodeOrder = DatabaseHelperMethods.SafeStr(reader, 24);

                return series;
            });
        }
        #endregion

        #region Seasons
        public List<WebSeason> GetAllSeasons(int _seriesId)
        {
            return GetSeasonsByNumber(_seriesId, null);
        }

        public WebSeason GetSeason(int _seriesId, int _seasonNumber)
        {
            return GetSeasonsByNumber(_seriesId, _seasonNumber).FirstOrDefault();
        }

        private List<WebSeason> GetSeasonsByNumber(int _seriesId, int? _seasonNumber)
        {
            string sql = "Select season.ID, season.SeriesID, season.SeasonIndex, season.CurrentBannerFileName, season.BannerFileNames, " +
                         "season.EpisodeCount, season.EpisodesUnWatched " +
                         "from season as season " +
                         "where season.Hidden = 0 and season.SeriesID=" + _seriesId +
                         (_seasonNumber != null ? (" and season.SeasonIndex=" + _seasonNumber.Value) : "");

            return ReadList<WebSeason>(sql, delegate(SQLiteDataReader reader)
            {
                WebSeason season = new WebSeason();
                season.Id = DatabaseHelperMethods.SafeStr(reader, 0);
                season.SeriesId = DatabaseHelperMethods.SafeInt32(reader, 1);
                season.SeasonNumber = DatabaseHelperMethods.SafeInt32(reader, 2);
                String currentBanner = DatabaseHelperMethods.SafeStr(reader, 3);
                season.SeasonBanner = CreateBannerUrl(currentBanner);
                String[] alternateBanners = Utils.SplitString(DatabaseHelperMethods.SafeStr(reader, 4));
                for (int i = 0; i < alternateBanners.Length; i++)
                {
                    alternateBanners[i] = CreateBannerUrl(alternateBanners[i]);
                }
                season.AlternateSeasonBanners = alternateBanners;
                season.EpisodesCount = DatabaseHelperMethods.SafeInt32(reader, 5);
                season.EpisodesCountUnwatched = DatabaseHelperMethods.SafeInt32(reader, 6);
                return season;
            });
        }
        #endregion

        #region Episodes
        public List<WebTVEpisodeBasic> GetAllEpisodes(int _seriesId)
        {
            return GetEpisodesForSeasonAndRange(_seriesId, null, null, null);
        }

        public List<WebTVEpisodeBasic> GetEpisodes(int _seriesId, int _start, int _end)
        {
            return GetEpisodesForSeasonAndRange(_seriesId, null, _start, _end);
        }

        public List<WebTVEpisodeBasic> GetEpisodesForSeason(int _seriesId, int _seasonId, int _start, int _end)
        {
            return GetEpisodesForSeasonAndRange(_seriesId, _seasonId, _start, _end);
        }

        public List<WebTVEpisodeBasic> GetAllEpisodesForSeason(int _seriesId, int _seasonNumber)
        {
            return GetEpisodesForSeasonAndRange(_seriesId, _seasonNumber, null, null);
        }

        private List<WebTVEpisodeBasic> GetEpisodesForSeasonAndRange(int _seriesId, int? _season, int? _start, int? _end)
        {
            List<String> alreadyDone = new List<String>();
            string sql = "Select online_eps.CompositeID, online_eps.EpisodeId, online_eps.SeriesID, online_eps.EpisodeName, online_eps.SeasonIndex, online_eps.EpisodeIndex, online_eps.Watched, online_eps.FirstAired, " +
                              "online_eps.thumbFilename, online_eps.Rating, online_eps.RatingCount, " +
                              "local_eps.IsAvailable, local_eps.videoWidth ,local_eps.videoHeight, local_eps.EpisodeFileName  " +
                              "FROM online_episodes as online_eps INNER JOIN local_episodes as local_eps " +
                              "ON online_eps.CompositeID=local_eps.CompositeID " +
                              "WHERE online_eps.SeriesID=" + _seriesId +
                              (_season != null ? (" and online_eps.SeasonIndex=" + _season) : ""); //also only get from one season
            return ReadList<WebTVEpisodeBasic>(sql, delegate(SQLiteDataReader reader)
            {
                string compositeId = DatabaseHelperMethods.SafeStr(reader, 0);
                if (alreadyDone.Contains(compositeId))
                    // episode has more than one file, we don't care atm -> only for full episode
                    return null;

                // we don't haven an entry for this episode yet -> add it
                WebTVEpisodeBasic episode = new WebTVEpisodeBasic();
                FillBasicEpisode(reader, episode);

                if (!reader.IsDBNull(11))
                {
                    episode.HasLocalFile = true;
                    String filename = DatabaseHelperMethods.SafeStr(reader, 14);
                    episode.Path = filename;
                }
                else
                {
                    episode.HasLocalFile = false;
                }

                return episode;
            });
        }

        public int GetEpisodesCount(int _seriesId)
        {
            return GetEpisodesCountForSeason(_seriesId, null);
        }

        public int GetEpisodesCountForSeason(int _seriesId, int? _season)
        {
            return GetEpisodesForSeasonAndRange(_seriesId, _season, null, null).Count;
        }

        public WebTVEpisodeDetailed GetFullEpisode(int _episodeId)
        {
            string sql = "Select online_eps.CompositeID, online_eps.EpisodeId, online_eps.SeriesID, online_eps.EpisodeName, online_eps.SeasonIndex, online_eps.EpisodeIndex, online_eps.Watched, online_eps.FirstAired, " + //basic episode
                              "online_eps.thumbFilename, online_eps.Rating, online_eps.RatingCount, " + //basic episode
                              "online_eps.Summary ,online_eps.GuestStars ,online_eps.Director ,online_eps.Writer ,online_eps.lastupdated ,online_eps.IMDB_ID ,online_eps.ProductionCode, " + //full episode
                              "online_eps.Combined_episodenumber ,online_eps.Combined_season ,online_eps.DVD_chapter ,online_eps.DVD_discid ,online_eps.DVD_episodenumber ,online_eps.DVD_season, " + //full episode
                              "online_eps.absolute_number ,online_eps.airsafter_season ,online_eps.airsbefore_episode ,online_eps.airsbefore_season, " + //full episode
                              "local_eps.EpisodeFileName ,local_eps.EpisodeIndex ,local_eps.SeasonIndex ,local_eps.IsAvailable ,local_eps.IsAvailable ,local_eps.localPlaytime, " + //full episode
                              "local_eps.videoWidth, local_eps.videoHeight, local_eps.VideoCodec, local_eps.VideoBitrate, local_eps.VideoFrameRate, " + //full episode
                              "local_eps.AudioCodec, local_eps.AudioBitrate, local_eps.AudioChannels, local_eps.AudioTracks, local_eps.AvailableSubtitles  " + //full episode
                              "FROM online_episodes as online_eps " +
                              "INNER JOIN local_episodes as local_eps " +
                              "ON online_eps.CompositeID=local_eps.CompositeID " +
                              "WHERE online_eps.Hidden = 0 AND online_eps.EpisodeID = " + _episodeId;
            //Todo: Remove "Removable" (changed to 2nd IsAvailable for now) since it doesn't exist anymore

            WebTVEpisodeDetailed episode = null;
            ReadList<int>(sql, delegate(SQLiteDataReader reader)
            {
                if (episode == null)
                {
                    // we don't have an entry for this episode yet -> add it
                    episode = new WebTVEpisodeDetailed();
                    FillBasicEpisode(reader, episode);
                    FillFullEpisode(reader, episode);

                    if (!reader.IsDBNull(31))
                    {
                        episode.HasLocalFile = true;
                        WebTVEpisodeDetailed.WebEpisodeFile file = CreateEpisodeFile(reader);
                        episode.EpisodeFile = file;
                    }
                    else
                    {
                        episode.HasLocalFile = false;
                    }
                }
                else
                {
                    // episode has more than one file, atm only up to 2 files supported
                    WebTVEpisodeDetailed.WebEpisodeFile file = CreateEpisodeFile(reader);
                    episode.EpisodeFile2 = file;
                }

                // creating a generic of a void isn't possible, so just return something
                return 1;
            });

            return episode;
        }

        private void FillBasicEpisode(SQLiteDataReader reader, WebTVEpisodeBasic episode)
        {
            try
            {
                episode.Id = DatabaseHelperMethods.SafeStr(reader, 1);
                episode.ShowId = DatabaseHelperMethods.SafeStr(reader, 2);
                episode.Title = DatabaseHelperMethods.SafeStr(reader, 3);
                episode.SeasonId = DatabaseHelperMethods.SafeStr(reader, 4);
                episode.EpisodeNumber = DatabaseHelperMethods.SafeInt32(reader, 5);
                episode.Watched = DatabaseHelperMethods.SafeBoolean(reader, 6);
                
                String bannerUrl = DatabaseHelperMethods.SafeStr(reader, 8);
                episode.BannerPath = CreateBannerUrl(bannerUrl);
                episode.Rating = DatabaseHelperMethods.SafeFloat(reader, 9);
                episode.RatingCount = DatabaseHelperMethods.SafeInt32(reader, 10);
            }
            catch (Exception ex)
            {
                Log.Error("Error reading episode information", ex);
            }
        }

        private void FillFullEpisode(SQLiteDataReader reader, WebTVEpisodeDetailed episode)
        {
            try
            {
                episode.FirstAired = DatabaseHelperMethods.SafeDateTime(reader, 7);
                episode.Summary = DatabaseHelperMethods.SafeStr(reader, 11);
                episode.GuestStarsString = DatabaseHelperMethods.SafeStr(reader, 12);
                episode.GuestStars = Utils.SplitString(episode.GuestStarsString).ToList();
                episode.DirectorsString = DatabaseHelperMethods.SafeStr(reader, 13);
                episode.Directors = Utils.SplitString(episode.DirectorsString).ToList();
                episode.WritersString = DatabaseHelperMethods.SafeStr(reader, 14);
                episode.Writers = Utils.SplitString(episode.WritersString).ToList();
                episode.LastUpdated = DatabaseHelperMethods.SafeDateTime(reader, 15);
                episode.ImdbId = DatabaseHelperMethods.SafeStr(reader, 16);
                episode.ProductionCode = DatabaseHelperMethods.SafeStr(reader, 17);

                episode.CombinedEpisodeNumber = DatabaseHelperMethods.SafeInt32(reader, 18);
                episode.CombinedSeasonNumber = DatabaseHelperMethods.SafeInt32(reader, 19);
                episode.DvdChapter = DatabaseHelperMethods.SafeInt32(reader, 20);
                episode.DvdDiscid = DatabaseHelperMethods.SafeInt32(reader, 21);
                episode.DvdEpisodenumber = DatabaseHelperMethods.SafeInt32(reader, 22);
                episode.DvdSeason = DatabaseHelperMethods.SafeInt32(reader, 23);

                episode.AbsoluteEpisodeNumber = DatabaseHelperMethods.SafeInt32(reader, 24);
                episode.AirsAfterSeason = DatabaseHelperMethods.SafeInt32(reader, 25);
                episode.AirsBeforeEpisode = DatabaseHelperMethods.SafeInt32(reader, 26);
                episode.AirsBeforeSesaon = DatabaseHelperMethods.SafeInt32(reader, 27);
            }
            catch (Exception ex)
            {
                Log.Error("Error reading episode details", ex);
            }

        }

        private WebTVEpisodeDetailed.WebEpisodeFile CreateEpisodeFile(SQLiteDataReader reader)
        {
            try
            {
                WebTVEpisodeDetailed.WebEpisodeFile file = new WebTVEpisodeDetailed.WebEpisodeFile();
                file.FileName = DatabaseHelperMethods.SafeStr(reader, 28);
                file.EpisodeIndex = DatabaseHelperMethods.SafeInt32(reader, 29);
                file.SeasonIndex = DatabaseHelperMethods.SafeInt32(reader, 30);
                file.IsAvailable = DatabaseHelperMethods.SafeBoolean(reader, 31);
                file.IsRemovable = DatabaseHelperMethods.SafeBoolean(reader, 32);
                file.Duration = DatabaseHelperMethods.SafeInt32(reader, 33);
                file.VideoWidth = DatabaseHelperMethods.SafeInt32(reader, 34);
                file.VideoHeight = DatabaseHelperMethods.SafeInt32(reader, 35);

                file.VideoCodec = DatabaseHelperMethods.SafeStr(reader, 36);
                file.VideoBitrate = DatabaseHelperMethods.SafeInt32(reader, 37);
                file.VideoFrameRate = DatabaseHelperMethods.SafeFloat(reader, 38);
                file.AudioCodec = DatabaseHelperMethods.SafeStr(reader, 39);
                file.AudioBitrate = DatabaseHelperMethods.SafeInt32(reader, 40);
                file.AudioChannels = DatabaseHelperMethods.SafeInt32(reader, 41);
                file.AudioTracks = DatabaseHelperMethods.SafeInt32(reader, 42);
                file.HasSubtitles = DatabaseHelperMethods.SafeBoolean(reader, 43);

                return file;
            }
            catch (Exception ex)
            {
                Log.Error("Error reading episode file details", ex);
                return null;
            }
        }

        private String CreateBannerUrl(String _banner)
        {
            return System.IO.Path.Combine(Utils.GetBannerPath("tvseries"), _banner.Replace('/', '\\'));
        }

        private String CreateFanartUrl(String _banner)
        {
            return System.IO.Path.Combine(Utils.GetBannerPath("fanart"), _banner.Replace('/', '\\'));
        }
        #endregion


        /// <summary>
        /// Gets the path to a media item
        /// </summary>
        /// <param name="itemId">Id of the media item</param>
        /// <returns>Path to the mediaitem or null if item id doesn't exist</returns>
        internal string GetSeriesPath(string itemId)
        {
            try
            {
                WebTVEpisodeDetailed ep = GetFullEpisode(Int32.Parse(itemId));
                if (ep != null)
                {
                    return ep.EpisodeFile.FileName;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error getting episode path for " + itemId, ex);
            }
            return null;
        }
    }
}