﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls.Platform
{
	internal partial class ModalNavigationManager
	{
		WindowRootViewContainer Container =>
			_window.NativeWindow.Content as WindowRootViewContainer ??
			throw new InvalidOperationException("Root container Panel not found");

		Task WindowReadyForModal()
		{
			if (CurrentPlatformPage.Handler is IPlatformViewHandler pvh &&
				pvh.PlatformView != null)
			{
				return pvh.PlatformView.OnLoadedAsync();
			}

			throw new InvalidOperationException("Page not initialized");
		}

		Task RemoveModalPage(Page page)
		{
			_platformModalPages.Remove(page);
			RemovePage(page, true);
			return Task.CompletedTask;
		}

		Task<Page> PopModalPlatformAsync(bool animated)
		{
			var tcs = new TaskCompletionSource<Page>();
			var poppedPage = CurrentPlatformModalPage;
			_platformModalPages.Remove(poppedPage);
			SetCurrent(CurrentPlatformPage, poppedPage, true, () => tcs.SetResult(poppedPage));
			return tcs.Task;
		}

		Task PushModalPlatformAsync(Page modal, bool animated)
		{
			_ = modal ?? throw new ArgumentNullException(nameof(modal));

			var tcs = new TaskCompletionSource<bool>();
			var currentPage = CurrentPlatformPage;
			_platformModalPages.Add(modal);
			SetCurrent(modal, currentPage, false, () => tcs.SetResult(true));
			return tcs.Task;
		}

		void RemovePage(Page page, bool popping)
		{
			if (page == null)
				return;

			var mauiContext = page.FindMauiContext() ??
				throw new InvalidOperationException("Maui Context removed from outgoing page too early");

			var windowManager = mauiContext.GetNavigationRootManager();
			Container.RemovePage(windowManager.RootView);

			if (popping)
			{
				page
					.FindMauiContext()
					?.GetNavigationRootManager()
					?.Disconnect();

				page.Handler?.DisconnectHandler();
				//page.Handler = null;

				// Un-parent the page; otherwise the Resources Changed Listeners won't be unhooked and the
				// page will leak
				//page.Parent = null;
			}
		}

		void SetCurrent(
			Page newPage,
			Page previousPage,
			bool popping,
			Action? completedCallback = null)
		{
			try
			{
				if (popping)
				{
					RemovePage(previousPage, popping);
				}
				else if (newPage.BackgroundColor.IsDefault() && newPage.Background.IsEmpty)
				{
					RemovePage(previousPage, popping);
				}

				if (Container == null || newPage == null)
					return;

				// pushing modal
				if (!popping)
				{
					var modalContext =
						WindowMauiContext
							.MakeScoped(registerNewNavigationRoot: true);

					newPage.Toolbar ??= new Toolbar(newPage);
					_ = newPage.Toolbar.ToPlatform(modalContext);

					var windowManager = modalContext.GetNavigationRootManager();
					windowManager.Connect(newPage.ToPlatform(modalContext));
					Container.AddPage(windowManager.RootView);

					previousPage
						.FindMauiContext()
						?.GetNavigationRootManager()
						?.UpdateAppTitleBar(false);
				}
				// popping modal
				else
				{
					var windowManager = newPage.FindMauiContext()?.GetNavigationRootManager() ??
						throw new InvalidOperationException("Previous Page Has Lost its MauiContext");

					Container.AddPage(windowManager.RootView);

					windowManager.UpdateAppTitleBar(true);
				}

				completedCallback?.Invoke();
			}
			catch (Exception error) when (error.HResult == -2147417842)
			{
				//This exception prevents the Main Page from being changed in a child
				//window or a different thread, except on the Main thread.
				//HEX 0x8001010E 
				throw new InvalidOperationException(
					"Changing the current page is only allowed if it's being called from the same UI thread." +
					"Please ensure that the new page is in the same UI thread as the current page.", error);
			}
		}
	}
}
