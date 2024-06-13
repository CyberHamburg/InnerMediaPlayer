using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Models;
using InnerMediaPlayer.Models.Search;
using InnerMediaPlayer.Tools;
using InnerMediaPlayer.UI;
using LitJson;
using UnityEngine;
using UnityEngine.UI;
using Network = InnerMediaPlayer.Tools.Network;

namespace InnerMediaPlayer.Logical
{
    internal class PlaylistUtility
    {
        private readonly string _fileLocation;
        private readonly Network _network;
        private readonly PlayingList _playingList;
        private readonly StringBuilder _stringBuilder;
        private readonly TaskQueue<int, bool> _iterateSongListTaskQueue;

        public PlaylistUtility(Network network, TaskQueue<int, bool> iterateSongListTaskQueue, PlayingList playingList)
        {
            _network = network;
            _iterateSongListTaskQueue = iterateSongListTaskQueue;
            _playingList = playingList;

            _fileLocation = Path.Combine(Application.persistentDataPath, "Playlist.json");
            _stringBuilder = new StringBuilder(25);
        }

        private static async Task<string> LoadFromFileAsync(string location)
        {
            if (!File.Exists(location))
                return string.Empty;
            using FileStream file = File.OpenRead(location);
            if (!file.CanRead)
                throw new FileLoadException($"没有读取路径{location}的权限");
            byte[] buffer = new byte[file.Length];
            int readCount = await file.ReadAsync(buffer, 0, buffer.Length);
            if (readCount == 0)
                throw new IOException("读取错误");
            string content = Encoding.UTF8.GetString(buffer);
            return content;
        }

        internal async Task<bool> LoadFromFileAsync(Text text, Lyric lyric, PlayList playList, NowPlaying nowPlaying, Action<string> setPreferredSize)
        {
            string json = await LoadFromFileAsync(_fileLocation);
            if (string.IsNullOrEmpty(json))
            {
                setPreferredSize(LocalList.FileDontExistTip);
                return false;
            }
            List<PlaylistJsonData.Cell> cells = JsonMapper.ToObject<PlaylistJsonData>(json).AllSongs;
            if (cells.Count == 0)
            {
                setPreferredSize(LocalList.FileDontExistTip);
                return false;
            }
            _stringBuilder.Append(text.text);
            _stringBuilder.Replace("{1}", cells.Count.ToString());
            string format = _stringBuilder.ToString();
            bool isSucceed = true;
            for (int i = 0; i < cells.Count; i++)
            {
                PlaylistJsonData.Cell cell = cells[i];
                _stringBuilder.Clear();
                _stringBuilder.Append(format);
                _stringBuilder.Replace("{0}", (i + 1).ToString());
                text.text = _stringBuilder.ToString();
                Sprite sprite = await _network.GetAlbum(cell.PictureUrl);
                bool playNow = i == cells.Count - 1;
                SongResult songResult = await _network.GetSongResultDetailAsync(cell.Id);
                isSucceed &= await AddAsync(playNow, cell.Id, cell.Name, cell.Artist, cell.PictureUrl, sprite, lyric, playList, nowPlaying, songResult);
                if (isSucceed) 
                    continue;
                setPreferredSize($"{LocalList.DownloadSongError}, 加载此歌曲时出错: Name:{cell.Name}, Id:{cell.Id}");
                return false;
            }

            return true;
        }

        internal async Task Save()
        {
            using FileStream fileStream = File.Exists(_fileLocation) ? File.OpenWrite(_fileLocation) : File.Create(_fileLocation);
            PlaylistJsonData jsonData = new PlaylistJsonData{AllSongs = new List<PlaylistJsonData.Cell>(_playingList.Count)};
            jsonData.AllSongs.AddRange(_playingList.Select(song => new PlaylistJsonData.Cell
                { Artist = song._artist, Id = song._id, Name = song._songName, PictureUrl = song._albumUrl }));
            JsonWriter jsonWriter = new JsonWriter { PrettyPrint = true };
            JsonMapper.ToJson(jsonData, jsonWriter);
            string json = System.Text.RegularExpressions.Regex.Unescape(jsonWriter.ToString());
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            if (!fileStream.CanSeek)
                throw new IOException($"没有对路径{_fileLocation}的Seek权限");
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.SetLength(0);
            if (!fileStream.CanWrite)
                throw new IOException($"没有对路径{_fileLocation}的Write权限");
            await fileStream.WriteAsync(buffer, 0, buffer.Length);

        }

        internal async Task<bool> PlayAsync(int id, string songName, string artist, string albumUrl, Sprite album, Lyric lyric, PlayList playList, NowPlaying nowPlaying, SongResult songResult)
        {
            AudioClip clip;
            try
            {
                clip = await _network.GetAudioClipAsync(songResult);
                await lyric.InstantiateLyricAsync(id, album.texture);
            }
            catch (Exception)
            {
                return false;
            }

            if (clip != null)
            {
                int disposedSongId = playList.ForceAdd(id, songName, artist, albumUrl, clip, album, playList.ScrollRect.content, lyric.Dispose);
                _iterateSongListTaskQueue.AddTask(disposedSongId, true, IterationListAsync);
                return true;
            }
#if UNITY_DEBUG
            Debug.Log($"发生未知错误导致不能播放:{songName}");
#endif

            return false;
            Task IterationListAsync(int disposeSongId, bool stopByForce, CancellationToken token) =>
                playList.IterationListAsync(nowPlaying.UpdateUI, lyric, disposeSongId, stopByForce, token);
        }

        internal async Task<bool> AddAsync(bool playNow, int id, string songName, string artist, string albumUrl, Sprite album, Lyric lyric, PlayList playList, NowPlaying nowPlaying, SongResult songResult)
        {
            AudioClip clip;
            try
            {
                clip = await _network.GetAudioClipAsync(songResult);
                await lyric.InstantiateLyricAsync(id, album.texture);
            }
            catch (Exception)
            {
                return false;
            }
            if (clip != null)
            {
                playList.AddToList(id, songName, artist, albumUrl, clip, album, playList.ScrollRect.content, lyric.Dispose);
                if (playNow)
                    _iterateSongListTaskQueue.AddTask(default, false, IterationListAsync);
                return true;
            }
#if UNITY_DEBUG
            Debug.Log($"发生未知错误导致不能播放:{songName}");
#endif

            return false;
            Task IterationListAsync(int disposedSongId, bool stopByForce, CancellationToken token) =>
                playList.IterationListAsync(nowPlaying.UpdateUI, lyric, disposedSongId, stopByForce, token);
        }

        /// <summary>
        /// 如果有任一歌手或名字完全匹配或部分匹配则优先展示，完全匹配的优先度最高
        /// </summary>
        /// <param name="result"></param>
        /// <param name="requestString"></param>
        internal static void SortByRelationship(SearchedResult result, string requestString)
        {
            List<SongsItem> songs = result.result.songs;
            string lower = requestString.ToLower();

            //按照优先级排序
            (int, int)[] indexArray = new (int, int)[songs.Count];
            for (int i = 0; i < songs.Count; i++)
            {
                //完全匹配优先级最高，则不考虑其他情况
                if (lower.Equals(songs[i].name.ToLower()))
                {
                    indexArray[i] = (1, i);
                    continue;
                }
                //包含则优先级低些
                if (songs[i].name.ToLower().Contains(lower))
                    indexArray[i] = (3, i);

                List<ArItem> arItems = songs[i].ar;
                //查找作者匹配度
                foreach (ArItem arItem in arItems)
                {
                    if (lower.Equals(arItem.name.ToLower()))
                    {
                        indexArray[i] = (2, i);
                        break;
                    }
                    if (arItem.name.ToLower().Contains(lower) && indexArray[i].Item1 == 0)
                        indexArray[i] = (4, i);
                }

                if (indexArray[i].Item1 == 0)
                    indexArray[i].Item2 = i;
            }

            int lastIndex = songs.Count - 1;
            int startIndex = 0;
            using IEnumerator<(int, int)> iEnumerator = indexArray.OrderBy(item => item.Item1).GetEnumerator();
            List<SongsItem> songItems = new List<SongsItem>(songs);
            //根据优先度排序
            while (iEnumerator.MoveNext())
            {
                var (priority, index) = iEnumerator.Current;
                if (priority == 0)
                {
                    songItems[lastIndex] = songs[index];
                    lastIndex--;
                }
                else
                {
                    songItems[startIndex] = songs[index];
                    startIndex++;
                }
            }

            result.result.songs = songItems;
        }
    }
}
