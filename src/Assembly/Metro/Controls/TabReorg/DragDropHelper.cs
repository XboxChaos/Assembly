using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DragDropListBox
{
	public class DragDropHelper
	{
		// source and target
		private static DragDropHelper instance;

		public static readonly DependencyProperty IsDragSourceProperty =
			DependencyProperty.RegisterAttached("IsDragSource", typeof (bool), typeof (DragDropHelper),
				new UIPropertyMetadata(false, IsDragSourceChanged));

		public static readonly DependencyProperty IsDropTargetProperty =
			DependencyProperty.RegisterAttached("IsDropTarget", typeof (bool), typeof (DragDropHelper),
				new UIPropertyMetadata(false, IsDropTargetChanged));

		public static readonly DependencyProperty DragDropTemplateProperty =
			DependencyProperty.RegisterAttached("DragDropTemplate", typeof (DataTemplate), typeof (DragDropHelper),
				new UIPropertyMetadata(null));

		private readonly DataFormat format = DataFormats.GetDataFormat("DragDropItemsControl");
		private DraggedAdorner draggedAdorner;
		private object draggedData;
		private bool hasVerticalOrientation;
		private Vector initialMouseOffset;
		private Point initialMousePosition;
		private InsertionAdorner insertionAdorner;
		private int insertionIndex;
		private bool isInFirstHalf;
		private FrameworkElement sourceItemContainer;
		private ItemsControl sourceItemsControl;
		private FrameworkElement targetItemContainer;
		private ItemsControl targetItemsControl;
		private Window topWindow;
		// singleton

		private static DragDropHelper Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new DragDropHelper();
				}
				return instance;
			}
		}

		public static bool GetIsDragSource(DependencyObject obj)
		{
			return (bool) obj.GetValue(IsDragSourceProperty);
		}

		public static void SetIsDragSource(DependencyObject obj, bool value)
		{
			obj.SetValue(IsDragSourceProperty, value);
		}


		public static bool GetIsDropTarget(DependencyObject obj)
		{
			return (bool) obj.GetValue(IsDropTargetProperty);
		}

		public static void SetIsDropTarget(DependencyObject obj, bool value)
		{
			obj.SetValue(IsDropTargetProperty, value);
		}

		public static DataTemplate GetDragDropTemplate(DependencyObject obj)
		{
			return (DataTemplate) obj.GetValue(DragDropTemplateProperty);
		}

		public static void SetDragDropTemplate(DependencyObject obj, DataTemplate value)
		{
			obj.SetValue(DragDropTemplateProperty, value);
		}

		private static void IsDragSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var dragSource = obj as ItemsControl;
			if (dragSource != null)
			{
				if (Equals(e.NewValue, true))
				{
					dragSource.PreviewMouseLeftButtonDown += Instance.DragSource_PreviewMouseLeftButtonDown;
					dragSource.PreviewMouseLeftButtonUp += Instance.DragSource_PreviewMouseLeftButtonUp;
					dragSource.PreviewMouseMove += Instance.DragSource_PreviewMouseMove;
				}
				else
				{
					dragSource.PreviewMouseLeftButtonDown -= Instance.DragSource_PreviewMouseLeftButtonDown;
					dragSource.PreviewMouseLeftButtonUp -= Instance.DragSource_PreviewMouseLeftButtonUp;
					dragSource.PreviewMouseMove -= Instance.DragSource_PreviewMouseMove;
				}
			}
		}

		private static void IsDropTargetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var dropTarget = obj as ItemsControl;
			if (dropTarget != null)
			{
				if (Equals(e.NewValue, true))
				{
					dropTarget.AllowDrop = true;
					dropTarget.PreviewDrop += Instance.DropTarget_PreviewDrop;
					dropTarget.PreviewDragEnter += Instance.DropTarget_PreviewDragEnter;
					dropTarget.PreviewDragOver += Instance.DropTarget_PreviewDragOver;
					dropTarget.PreviewDragLeave += Instance.DropTarget_PreviewDragLeave;
				}
				else
				{
					dropTarget.AllowDrop = false;
					dropTarget.PreviewDrop -= Instance.DropTarget_PreviewDrop;
					dropTarget.PreviewDragEnter -= Instance.DropTarget_PreviewDragEnter;
					dropTarget.PreviewDragOver -= Instance.DropTarget_PreviewDragOver;
					dropTarget.PreviewDragLeave -= Instance.DropTarget_PreviewDragLeave;
				}
			}
		}

		// DragSource

		private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			sourceItemsControl = (ItemsControl) sender;
			var visual = e.OriginalSource as Visual;
			if (visual == null)
				return;

			topWindow = Window.GetWindow(sourceItemsControl);
			initialMousePosition = e.GetPosition(topWindow);

			sourceItemContainer = sourceItemsControl.ContainerFromElement(visual) as FrameworkElement;
			if (sourceItemContainer != null)
			{
				draggedData = sourceItemContainer.DataContext;
			}
		}

		// Drag = mouse down + move by a certain amount
		private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (draggedData != null)
			{
				// Only drag when user moved the mouse by a reasonable amount.
				if (Utilities.IsMovementBigEnough(initialMousePosition, e.GetPosition(topWindow)))
				{
					initialMouseOffset = initialMousePosition - sourceItemContainer.TranslatePoint(new Point(0, 0), topWindow);

					var data = new DataObject(format.Name, draggedData);

					// Adding events to the window to make sure dragged adorner comes up when mouse is not over a drop target.
					bool previousAllowDrop = topWindow.AllowDrop;
					topWindow.AllowDrop = true;
					topWindow.DragEnter += TopWindow_DragEnter;
					topWindow.DragOver += TopWindow_DragOver;
					topWindow.DragLeave += TopWindow_DragLeave;

					DragDropEffects effects = DragDrop.DoDragDrop((DependencyObject) sender, data, DragDropEffects.Move);

					// Without this call, there would be a bug in the following scenario: Click on a data item, and drag
					// the mouse very fast outside of the window. When doing this really fast, for some reason I don't get 
					// the Window leave event, and the dragged adorner is left behind.
					// With this call, the dragged adorner will disappear when we release the mouse outside of the window,
					// which is when the DoDragDrop synchronous method returns.
					RemoveDraggedAdorner();

					topWindow.AllowDrop = previousAllowDrop;
					topWindow.DragEnter -= TopWindow_DragEnter;
					topWindow.DragOver -= TopWindow_DragOver;
					topWindow.DragLeave -= TopWindow_DragLeave;

					draggedData = null;
				}
			}
		}

		private void DragSource_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			draggedData = null;
		}

		// DropTarget

		private void DropTarget_PreviewDragEnter(object sender, DragEventArgs e)
		{
			targetItemsControl = (ItemsControl) sender;
			object draggedItem = e.Data.GetData(format.Name);

			DecideDropTarget(e);
			if (draggedItem != null)
			{
				// Dragged Adorner is created on the first enter only.
				ShowDraggedAdorner(e.GetPosition(topWindow));
				CreateInsertionAdorner();
			}
			e.Handled = true;
		}

		private void DropTarget_PreviewDragOver(object sender, DragEventArgs e)
		{
			object draggedItem = e.Data.GetData(format.Name);

			DecideDropTarget(e);
			if (draggedItem != null)
			{
				// Dragged Adorner is only updated here - it has already been created in DragEnter.
				ShowDraggedAdorner(e.GetPosition(topWindow));
				UpdateInsertionAdornerPosition();
			}
			e.Handled = true;
		}

		private void DropTarget_PreviewDrop(object sender, DragEventArgs e)
		{
			object draggedItem = e.Data.GetData(format.Name);
			int indexRemoved = -1;

			if (draggedItem != null)
			{
				if ((e.Effects & DragDropEffects.Move) != 0)
				{
					indexRemoved = Utilities.RemoveItemFromItemsControl(sourceItemsControl, draggedItem);
				}
				// This happens when we drag an item to a later position within the same ItemsControl.
				if (indexRemoved != -1 && sourceItemsControl == targetItemsControl && indexRemoved < insertionIndex)
				{
					insertionIndex--;
				}
				Utilities.InsertItemInItemsControl(targetItemsControl, draggedItem, insertionIndex);

				RemoveDraggedAdorner();
				RemoveInsertionAdorner();
			}
			e.Handled = true;
		}

		private void DropTarget_PreviewDragLeave(object sender, DragEventArgs e)
		{
			// Dragged Adorner is only created once on DragEnter + every time we enter the window. 
			// It's only removed once on the DragDrop, and every time we leave the window. (so no need to remove it here)
			object draggedItem = e.Data.GetData(format.Name);

			if (draggedItem != null)
			{
				RemoveInsertionAdorner();
			}
			e.Handled = true;
		}

		// If the types of the dragged data and ItemsControl's source are compatible, 
		// there are 3 situations to have into account when deciding the drop target:
		// 1. mouse is over an items container
		// 2. mouse is over the empty part of an ItemsControl, but ItemsControl is not empty
		// 3. mouse is over an empty ItemsControl.
		// The goal of this method is to decide on the values of the following properties: 
		// targetItemContainer, insertionIndex and isInFirstHalf.
		private void DecideDropTarget(DragEventArgs e)
		{
			int targetItemsControlCount = targetItemsControl.Items.Count;
			object draggedItem = e.Data.GetData(format.Name);

			if (IsDropDataTypeAllowed(draggedItem))
			{
				if (targetItemsControlCount > 0)
				{
					hasVerticalOrientation =
						Utilities.HasVerticalOrientation(
							targetItemsControl.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement);
					targetItemContainer =
						targetItemsControl.ContainerFromElement((DependencyObject) e.OriginalSource) as FrameworkElement;

					if (targetItemContainer != null)
					{
						Point positionRelativeToItemContainer = e.GetPosition(targetItemContainer);
						isInFirstHalf = Utilities.IsInFirstHalf(targetItemContainer, positionRelativeToItemContainer,
							hasVerticalOrientation);
						insertionIndex = targetItemsControl.ItemContainerGenerator.IndexFromContainer(targetItemContainer);

						if (!isInFirstHalf)
						{
							insertionIndex++;
						}
					}
					else
					{
						targetItemContainer =
							targetItemsControl.ItemContainerGenerator.ContainerFromIndex(targetItemsControlCount - 1) as FrameworkElement;
						isInFirstHalf = false;
						insertionIndex = targetItemsControlCount;
					}
				}
				else
				{
					targetItemContainer = null;
					insertionIndex = 0;
				}
			}
			else
			{
				targetItemContainer = null;
				insertionIndex = -1;
				e.Effects = DragDropEffects.None;
			}
		}

		// Can the dragged data be added to the destination collection?
		// It can if destination is bound to IList<allowed type>, IList or not data bound.
		private bool IsDropDataTypeAllowed(object draggedItem)
		{
			bool isDropDataTypeAllowed;
			IEnumerable collectionSource = targetItemsControl.ItemsSource;
			if (draggedItem != null)
			{
				if (collectionSource != null)
				{
					Type draggedType = draggedItem.GetType();
					Type collectionType = collectionSource.GetType();

					Type genericIListType = collectionType.GetInterface("IList`1");
					if (genericIListType != null)
					{
						Type[] genericArguments = genericIListType.GetGenericArguments();
						isDropDataTypeAllowed = genericArguments[0].IsAssignableFrom(draggedType);
					}
					else if (typeof (IList).IsAssignableFrom(collectionType))
					{
						isDropDataTypeAllowed = true;
					}
					else
					{
						isDropDataTypeAllowed = false;
					}
				}
				else // the ItemsControl's ItemsSource is not data bound.
				{
					isDropDataTypeAllowed = true;
				}
			}
			else
			{
				isDropDataTypeAllowed = false;
			}
			return isDropDataTypeAllowed;
		}

		// Window

		private void TopWindow_DragEnter(object sender, DragEventArgs e)
		{
			ShowDraggedAdorner(e.GetPosition(topWindow));
			e.Effects = DragDropEffects.None;
			e.Handled = true;
		}

		private void TopWindow_DragOver(object sender, DragEventArgs e)
		{
			ShowDraggedAdorner(e.GetPosition(topWindow));
			e.Effects = DragDropEffects.None;
			e.Handled = true;
		}

		private void TopWindow_DragLeave(object sender, DragEventArgs e)
		{
			RemoveDraggedAdorner();
			e.Handled = true;
		}

		// Adorners

		// Creates or updates the dragged Adorner. 
		private void ShowDraggedAdorner(Point currentPosition)
		{
			if (draggedAdorner == null)
			{
				AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(sourceItemsControl);
				draggedAdorner = new DraggedAdorner(draggedData, GetDragDropTemplate(sourceItemsControl), sourceItemContainer,
					adornerLayer);
			}
			draggedAdorner.SetPosition(currentPosition.X - initialMousePosition.X + initialMouseOffset.X,
				currentPosition.Y - initialMousePosition.Y + initialMouseOffset.Y);
		}

		private void RemoveDraggedAdorner()
		{
			if (draggedAdorner != null)
			{
				draggedAdorner.Detach();
				draggedAdorner = null;
			}
		}

		private void CreateInsertionAdorner()
		{
			if (targetItemContainer != null)
			{
				// Here, I need to get adorner layer from targetItemContainer and not targetItemsControl. 
				// This way I get the AdornerLayer within ScrollContentPresenter, and not the one under AdornerDecorator (Snoop is awesome).
				// If I used targetItemsControl, the adorner would hang out of ItemsControl when there's a horizontal scroll bar.
				AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(targetItemContainer);
				insertionAdorner = new InsertionAdorner(hasVerticalOrientation, isInFirstHalf, targetItemContainer, adornerLayer);
			}
		}

		private void UpdateInsertionAdornerPosition()
		{
			if (insertionAdorner != null)
			{
				insertionAdorner.IsInFirstHalf = isInFirstHalf;
				insertionAdorner.InvalidateVisual();
			}
		}

		private void RemoveInsertionAdorner()
		{
			if (insertionAdorner != null)
			{
				insertionAdorner.Detach();
				insertionAdorner = null;
			}
		}
	}
}