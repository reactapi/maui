﻿using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Primitives;
using Xunit;
using static Microsoft.Maui.Controls.Core.UnitTests.VisualStateTestHelpers;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class VisualElementTests
	{
		[Fact("If WidthRequest has been set and is reset to -1, the Core Width should return to being Unset")]
		public void SettingWidthRequestToNegativeOneShouldResetWidth()
		{
			var visualElement = new Label();
			var coreView = visualElement as IView;

			Assert.Equal(Dimension.Unset, coreView.Width);
			Assert.False(visualElement.IsSet(VisualElement.WidthRequestProperty));

			double testWidth = 100;
			visualElement.WidthRequest = testWidth;

			Assert.Equal(coreView.Width, testWidth);
			Assert.True(visualElement.IsSet(VisualElement.WidthRequestProperty));
			Assert.Equal(visualElement.WidthRequest, testWidth);

			// -1 is the legacy "unset" value for WidthRequest; we want to support setting it back to -1 as a way 
			// to "reset" it to the "unset" value.
			visualElement.WidthRequest = -1;

			Assert.Equal(Dimension.Unset, coreView.Width);
			Assert.Equal(-1, visualElement.WidthRequest);
		}

		[Fact("If HeightRequest has been set and is reset to -1, the Core Height should return to being Unset")]
		public void SettingHeightRequestToNegativeOneShouldResetWidth()
		{
			var visualElement = new Label();
			var coreView = visualElement as IView;

			Assert.Equal(Dimension.Unset, coreView.Height);
			Assert.False(visualElement.IsSet(VisualElement.HeightRequestProperty));

			double testHeight = 100;
			visualElement.HeightRequest = testHeight;

			Assert.Equal(coreView.Height, testHeight);
			Assert.True(visualElement.IsSet(VisualElement.HeightRequestProperty));
			Assert.Equal(visualElement.HeightRequest, testHeight);

			// -1 is the legacy "unset" value for HeightRequest; we want to support setting it back to -1 as a way 
			// to "reset" it to the "unset" value.
			visualElement.HeightRequest = -1;

			Assert.Equal(Dimension.Unset, coreView.Height);
			Assert.Equal(-1, visualElement.HeightRequest);
		}

		[Fact]
		public void BindingContextPropagatesToBackground()
		{
			var visualElement = new Label();
			var brush = new LinearGradientBrush();
			visualElement.Background = brush;

			var bc1 = new object();
			visualElement.BindingContext = bc1;
			Assert.Equal(bc1, brush.BindingContext);

			var brush2 = new LinearGradientBrush();
			visualElement.Background = brush2;
			Assert.Equal(bc1, brush2.BindingContext);

		}

		[Fact]
		public void FocusedElementGetsFocusedVisualState()
		{
			var vsgList = CreateTestStateGroups();
			var stateGroup = vsgList[0];
			var element = new Button();
			VisualStateManager.SetVisualStateGroups(element, vsgList);

			element.SetValue(VisualElement.IsFocusedPropertyKey, true);
			Assert.Equal(FocusedStateName, stateGroup.CurrentState.Name);
		}

		[Fact]
		public void ContainerChangedFiresWhenMapContainerIsCalled()
		{
			var handlerStub = new HandlerStub((PropertyMapper)VisualElement.ControlsVisualElementMapper);
			var button = new Button();
			button.Handler = handlerStub;

			bool fired = false;
			(button as IControlsView).PlatformContainerViewChanged += (_, _) => fired = true;
			handlerStub.UpdateValue(nameof(IViewHandler.ContainerView));
			Assert.True(fired);
		}

		[Theory]
		[InlineData(typeof(ImmutableBrush), false)]
		[InlineData(typeof(SolidColorBrush), false)]
		[InlineData(typeof(LinearGradientBrush), true)]
		[InlineData(typeof(RadialGradientBrush), true)]
		public async Task BackgroundDoesNotLeak(Type type, bool defaultCtor)
		{
			var brush = defaultCtor ?
				(Brush)Activator.CreateInstance(type) :
				(Brush)Activator.CreateInstance(type, Colors.CornflowerBlue);

			var reference = new WeakReference(new VisualElement { Background = brush });

			await Task.Yield();
			GC.Collect();
			GC.WaitForPendingFinalizers();

			Assert.False(reference.IsAlive, "VisualElement should not be alive!");
		}

		[Fact]
		public void GradientBrushSubscribed()
		{
			var gradient = new LinearGradientBrush
			{
				GradientStops =
				{
					new GradientStop(Colors.White, 0),
					new GradientStop(Colors.CornflowerBlue, 1),
				}
			};
			var visual = new VisualElement { Background = gradient };

			bool fired = false;
			visual.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(VisualElement.Background))
					fired = true;
			};

			gradient.GradientStops.Add(new GradientStop(Colors.CornflowerBlue, 1));
			Assert.True(fired, "PropertyChanged did not fire!");
		}

		[Theory]
		[InlineData(typeof(RectangleGeometry))]
		[InlineData(typeof(EllipseGeometry))]
		public async Task ClipDoesNotLeak(Type type)
		{
			var geometry = (Geometry)Activator.CreateInstance(type);
			var reference = new WeakReference(new VisualElement { Clip = geometry });

			await Task.Yield();
			GC.Collect();
			GC.WaitForPendingFinalizers();

			Assert.False(reference.IsAlive, "VisualElement should not be alive!");
		}

		[Fact]
		public void RectangleGeometrySubscribed()
		{
			var geometry = new RectangleGeometry();
			var visual = new VisualElement { Clip = geometry };

			bool fired = false;
			visual.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(VisualElement.Clip))
					fired = true;
			};

			geometry.Rect = new Rect(1, 2, 3, 4);
			Assert.True(fired, "PropertyChanged did not fire!");
		}
	}
}
