using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nexus.Core;
using RingFlow.Gameplay.UI;

namespace RingFlow.Tests
{
    [TestFixture]
    public class ViewMediatorTests
    {
        // ── View Construction Tests ──────────────────────────────────────────

        [Test]
        public void SplashView_CanBeConstructed_WithoutScene()
        {
            var view = new SplashView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void MainMenuView_CanBeConstructed_WithoutScene()
        {
            var view = new MainMenuView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void HUDView_CanBeConstructed_WithoutScene()
        {
            var view = new HUDView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void PauseView_CanBeConstructed_WithoutScene()
        {
            var view = new PauseView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void WinView_CanBeConstructed_WithoutScene()
        {
            var view = new WinView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void GameOverView_CanBeConstructed_WithoutScene()
        {
            var view = new GameOverView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void LevelSelectView_CanBeConstructed_WithoutScene()
        {
            var view = new LevelSelectView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void SettingsView_CanBeConstructed_WithoutScene()
        {
            var view = new SettingsView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void ChestPopupView_CanBeConstructed_WithoutScene()
        {
            var view = new ChestPopupView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void DailyRewardPopupView_CanBeConstructed_WithoutScene()
        {
            var view = new DailyRewardPopupView();
            Assert.IsNotNull(view);
        }

        [Test]
        public void ParentalGatePopupView_CanBeConstructed_WithoutScene()
        {
            var view = new ParentalGatePopupView();
            Assert.IsNotNull(view);
        }

        // ── Mediator Construction Tests ──────────────────────────────────────

        [Test]
        public void SplashMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new SplashMediator());
        }

        [Test]
        public void MainMenuMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new MainMenuMediator());
        }

        [Test]
        public void HUDMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new HUDMediator());
        }

        [Test]
        public void PauseMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new PauseMediator());
        }

        [Test]
        public void WinMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new WinMediator());
        }

        [Test]
        public void GameOverMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new GameOverMediator());
        }

        [Test]
        public void LevelSelectMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new LevelSelectMediator());
        }

        [Test]
        public void SettingsMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new SettingsMediator());
        }

        [Test]
        public void ChestPopupMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new ChestPopupMediator());
        }

        [Test]
        public void DailyRewardPopupMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new DailyRewardPopupMediator());
        }

        [Test]
        public void ParentalGatePopupMediator_CanBeConstructed()
        {
            Assert.DoesNotThrow(() => new ParentalGatePopupMediator());
        }

        // ── OnBind/OnUnbind Smoke Tests ────────────────────────────────────

        [Test]
        public void SplashMediator_OnBind_NullView_DoesNotThrow()
        {
            var mediator = new SplashMediator();
            Assert.DoesNotThrow(() => InvokeOnBind(mediator));
        }

        [Test]
        public void SplashMediator_OnUnbind_DoesNotThrow()
        {
            var mediator = new SplashMediator();
            Assert.DoesNotThrow(() => InvokeOnUnbind(mediator));
        }

        [Test]
        public void MainMenuMediator_OnBind_NullView_DoesNotThrow()
        {
            var mediator = new MainMenuMediator();
            // OnBind tries to access View._signalBus - without DI, it's null, so shouldn't throw
            Assert.DoesNotThrow(() => InvokeOnBind(mediator));
        }

        [Test]
        public void MainMenuMediator_OnUnbind_DoesNotThrow()
        {
            var mediator = new MainMenuMediator();
            Assert.DoesNotThrow(() => InvokeOnUnbind(mediator));
        }

        [Test]
        public void HUDMediator_OnBind_NullView_DoesNotThrow()
        {
            var mediator = new HUDMediator();
            Assert.DoesNotThrow(() => InvokeOnBind(mediator));
        }

        [Test]
        public void HUDMediator_OnUnbind_DoesNotThrow()
        {
            var mediator = new HUDMediator();
            Assert.DoesNotThrow(() => InvokeOnUnbind(mediator));
        }

        [Test]
        public void PauseMediator_OnBind_NullView_DoesNotThrow()
        {
            var mediator = new PauseMediator();
            Assert.DoesNotThrow(() => InvokeOnBind(mediator));
        }

        [Test]
        public void WinMediator_OnBind_NullView_DoesNotThrow()
        {
            var mediator = new WinMediator();
            Assert.DoesNotThrow(() => InvokeOnBind(mediator));
        }

        [Test]
        public void GameOverMediator_OnBind_NullView_DoesNotThrow()
        {
            var mediator = new GameOverMediator();
            Assert.DoesNotThrow(() => InvokeOnBind(mediator));
        }

        [Test]
        public void LevelSelectMediator_OnBind_NullView_DoesNotThrow()
        {
            var mediator = new LevelSelectMediator();
            Assert.DoesNotThrow(() => InvokeOnBind(mediator));
        }

        [Test]
        public void SettingsMediator_OnBind_NullView_DoesNotThrow()
        {
            var mediator = new SettingsMediator();
            Assert.DoesNotThrow(() => InvokeOnBind(mediator));
        }

        private static void InvokeOnBind(object mediator)
        {
            var method = mediator.GetType().GetMethod("OnBind",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(mediator, null);
        }

        private static void InvokeOnUnbind(object mediator)
        {
            var method = mediator.GetType().GetMethod("OnUnbind",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(mediator, null);
        }
    }
}