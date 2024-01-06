using InnerMediaPlayer.Logical;
using InnerMediaPlayer.Management;
using InnerMediaPlayer.Management.UI;
using InnerMediaPlayer.Models.Signal;
using InnerMediaPlayer.Tools;
using LitJson.Extension;
using UnityEngine;
using Zenject;
using Network = InnerMediaPlayer.Tools.Network;
using PlayingList = InnerMediaPlayer.Logical.PlayingList;

namespace InnerMediaPlayer.Installer
{
    public class MediaPlayerInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);
            //懒得再写一个枚举了\-.-/
            Container.DeclareSignalWithInterfaces<LyricDisplaySignal>().WithId("Normal");
            Container.DeclareSignalWithInterfaces<LyricInterruptDisplaySignal>().WithId("Interruption");

            Container.BindInterfacesAndSelfTo<JsonRegister>().AsSingle();
            Container.Bind<Crypto>().ToSelf().AsSingle();
            Container.Bind<Cookies>().ToSelf().AsSingle();
            Container.Bind<Network>().ToSelf().AsSingle();
            Container.Bind<TextGenerator>().ToSelf().AsSingle();
            Container.Bind<Lyrics>().ToSelf().AsSingle();
            Container.Bind<TaskQueue>().ToSelf().AsTransient();
            Container.Bind(typeof(TaskQueue<>), typeof(TaskQueue<,>), typeof(TaskQueue<,,>)).AsTransient();

            Container.BindInterfacesAndSelfTo<PlayingList>().AsSingle();
            Container.BindInterfacesAndSelfTo<PrefabManager>().AsSingle();
            Container.Bind<GameSetting>().AsSingle().NonLazy();

            Container.BindFactory<float, string, Color, Transform, Lyrics.Line, Lyrics.Line.Factory>()
                .FromPoolableMemoryPool(x => x.WithInitialSize(30));
            Container.BindFactory<int, string, string, AudioClip, Sprite, PlayingList.Song, PlayingList.Song.Factory>()
                .FromPoolableMemoryPool();
            Container.BindFactory<int, string, string, Sprite, Transform, PlayingList.UIElement, PlayingList.UIElement.Factory>()
                .FromPoolableMemoryPool();

            Container.BindInterfacesAndSelfTo<UIManager>().AsSingle();
            Container.Bind<CoroutineQueue>().FromNewComponentOnNewGameObject()
                .WithGameObjectName(nameof(CoroutineQueue)).AsTransient();

            Container.BindExecutionOrder<UIManager>(-100);
            Container.BindExecutionOrder<JsonRegister>(-200);
        }
    }
}