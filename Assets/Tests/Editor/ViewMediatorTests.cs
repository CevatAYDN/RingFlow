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
            var go = new UnityEngine.GameObject("SplashView", typeof(UnityEngine.UI.Image));
            var view = go.AddComponent<SplashView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void MainMenuView_CanBeConstructed_WithoutScene()
        {
            var go = new UnityEngine.GameObject("MainMenuView");
            var view = go.AddComponent<MainMenuView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HUDView_CanBeConstructed_WithoutScene()
        {
            var go = new UnityEngine.GameObject("HUDView");
            var view = go.AddComponent<HUDView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void PauseView_CanBeConstructed_WithoutScene()
        {
            var go = new UnityEngine.GameObject("PauseView", typeof(UnityEngine.UI.Image));
            var view = go.AddComponent<PauseView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void WinView_CanBeConstructed_WithoutScene()
        {
            var go = new UnityEngine.GameObject("WinView", typeof(UnityEngine.UI.Image));
            var view = go.AddComponent<WinView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void GameOverView_CanBeConstructed_WithoutScene()
        {
            var go = new UnityEngine.GameObject("GameOverView", typeof(UnityEngine.UI.Image));
            var view = go.AddComponent<GameOverView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void LevelSelectView_CanBeConstructed_WithoutScene()
        {
            var go = new UnityEngine.GameObject("LevelSelectView");
            var view = go.AddComponent<LevelSelectView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void SettingsView_CanBeConstructed_WithoutScene()
        {
            var go = new UnityEngine.GameObject("SettingsView", typeof(UnityEngine.UI.Image));
            var view = go.AddComponent<SettingsView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ChestPopupView_CanBeConstructed_WithoutScene()
        {
            var go = new UnityEngine.GameObject("ChestPopupView", typeof(UnityEngine.UI.Image));
            var view = go.AddComponent<ChestPopupView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void DailyRewardPopupView_CanBeConstructed_WithoutScene()
        {
            var go = new UnityEngine.GameObject("DailyRewardPopupView", typeof(UnityEngine.UI.Image));
            var view = go.AddComponent<DailyRewardPopupView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ParentalGatePopupView_CanBeConstructed_WithoutScene()
        {
            var theme = UnityEngine.Resources.Load<RingFlow.Gameplay.UIThemeConfigSO>(RingFlow.Gameplay.GameplayAssetKeys.UIThemeConfig);
            if (theme != null) GameUIResources.Bind(theme);
            var go = new UnityEngine.GameObject("ParentalGatePopupView");
            var view = go.AddComponent<ParentalGatePopupView>();
            view.EnsureInitialized();
            Assert.IsNotNull(view);
            Assert.IsNotNull(view.AcceptButton, "Parental gate must always have an accept button so first-run users are not trapped.");
            Assert.IsNull(view.AgeInputField, "Parental gate is consent-only and must not require a math/input challenge.");
            Assert.IsNotNull(view.QuestionText, "Parental gate must show localized consent text.");
            Assert.IsTrue(view.ValidateAnswer(), "Consent-only parental gate should pass when the accept button is pressed.");
            UnityEngine.Object.DestroyImmediate(go);
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
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "[Nexus][SplashMediator][OnBind][UI] View not bound.");
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
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "[Nexus][HUDMediator][OnBind] HUDView not bound.");
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
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "[Nexus][WinMediator][OnBind] WinView not bound.");
            var mediator = new WinMediator();
            Assert.DoesNotThrow(() => InvokeOnBind(mediator));
        }

        [Test]
        public void GameOverMediator_OnBind_NullView_DoesNotThrow()
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "[Nexus][GameOverMediator][OnBind] GameOverView not bound.");
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