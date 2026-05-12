namespace Mosaic.UI.Wpf.Controls
{
    internal sealed class RelativePanelNode
    {
        private readonly DependencyObject _mElement;
        // Represents the space that's available to an element given its set of
        // constraints. The width and height of this rect is used to measure
        // a given element.
        public Rect MMeasureRect;

        // Represents the exact space within the MeasureRect that will be used 
        // to arrange a given element.
        public Rect MArrangeRect;

        private RelativePanelState _mState = RelativePanelState.Unresolved;

        // Indicates if this is the last element in a dependency chain that is
        // formed by only connecting nodes horizontally.
        public bool MIsHorizontalLeaf = true;

        // Indicates if this is the last element in a dependency chain that is
        // formed by only connecting nodes vertically.
        public bool MIsVerticalLeaf = true;

        public RelativePanelConstraints MConstraints = RelativePanelConstraints.None;
        public RelativePanelNode? MLeftOfNode;
        public RelativePanelNode? MAboveNode;
        public RelativePanelNode? MRightOfNode;
        public RelativePanelNode? MBelowNode;
        public RelativePanelNode? MAlignHorizontalCenterWithNode;
        public RelativePanelNode? MAlignVerticalCenterWithNode;
        public RelativePanelNode? MAlignLeftWithNode;
        public RelativePanelNode? MAlignTopWithNode;
        public RelativePanelNode? MAlignRightWithNode;
        public RelativePanelNode? MAlignBottomWithNode;

        public RelativePanelNode(DependencyObject element)
        {
            _mElement = element;
        }

        public DependencyObject Element => _mElement;

        public double DesiredWidth => _mElement is UIElement element ? element.DesiredSize.Width : 0;

        public double DesiredHeight => _mElement is UIElement element ? element.DesiredSize.Height : 0;

        public void Measure(Size constrainedAvailableSize)
        {
            if (_mElement is UIElement element)
            {
                element.Measure(constrainedAvailableSize);
            }
        }
        public void Arrange(Rect finalRect)
        {
            if (_mElement is UIElement element)
            {
                element.Arrange(finalRect);
            }
        }
        public object? GetLeftOfValue() => RelativePanel.GetLeftOf(_mElement);
        public object? GetAboveValue() => RelativePanel.GetAbove(_mElement);
        public object? GetRightOfValue() => RelativePanel.GetRightOf(_mElement);
        public object? GetBelowValue() => RelativePanel.GetBelow(_mElement);
        public object? GetAlignHorizontalCenterWithValue() => RelativePanel.GetAlignHorizontalCenterWith(_mElement);
        public object? GetAlignVerticalCenterWithValue() => RelativePanel.GetAlignVerticalCenterWith(_mElement);
        public object? GetAlignLeftWithValue() => RelativePanel.GetAlignLeftWith(_mElement);
        public object? GetAlignTopWithValue() => RelativePanel.GetAlignTopWith(_mElement);
        public object? GetAlignRightWithValue() => RelativePanel.GetAlignRightWith(_mElement);
        public object? GetAlignBottomWithValue() => RelativePanel.GetAlignBottomWith(_mElement);
        public bool GetAlignLeftWithPanelValue() => RelativePanel.GetAlignLeftWithPanel(_mElement);
        public bool GetAlignTopWithPanelValue() => RelativePanel.GetAlignTopWithPanel(_mElement);
        public bool GetAlignRightWithPanelValue() => RelativePanel.GetAlignRightWithPanel(_mElement);
        public bool GetAlignBottomWithPanelValue() => RelativePanel.GetAlignBottomWithPanel(_mElement);
        public bool GetAlignHorizontalCenterWithPanelValue() => RelativePanel.GetAlignHorizontalCenterWithPanel(_mElement);
        public bool GetAlignVerticalCenterWithPanelValue() => RelativePanel.GetAlignVerticalCenterWithPanel(_mElement);

        // The node is said to be anchored when its ArrangeRect is expected to
        // align with its MeasureRect on one or more sides. For example, if the 
        // node is right-anchored, the right side of the ArrangeRect should overlap
        // with the right side of the MeasureRect. Anchoring is determined by
        // specific combinations of dependencies.
        public bool IsLeftAnchored => (IsAlignLeftWithPanel || IsAlignLeftWith || (IsRightOf && !IsAlignHorizontalCenterWith));
        public bool IsTopAnchored => (IsAlignTopWithPanel || IsAlignTopWith || (IsBelow && !IsAlignVerticalCenterWith));
        public bool IsRightAnchored => (IsAlignRightWithPanel || IsAlignRightWith || (IsLeftOf && !IsAlignHorizontalCenterWith));
        public bool IsBottomAnchored => (IsAlignBottomWithPanel || IsAlignBottomWith || (IsAbove && !IsAlignVerticalCenterWith));
        public bool IsHorizontalCenterAnchored => ((IsAlignHorizontalCenterWithPanel && !IsAlignLeftWithPanel && !IsAlignRightWithPanel && !IsAlignLeftWith && !IsAlignRightWith && !IsLeftOf && !IsRightOf)
             || (IsAlignHorizontalCenterWith && !IsAlignLeftWithPanel && !IsAlignRightWithPanel && !IsAlignLeftWith && !IsAlignRightWith));
        public bool IsVerticalCenterAnchored => ((IsAlignVerticalCenterWithPanel && !IsAlignTopWithPanel && !IsAlignBottomWithPanel && !IsAlignTopWith && !IsAlignBottomWith && !IsAbove && !IsBelow)
             || (IsAlignVerticalCenterWith && !IsAlignTopWithPanel && !IsAlignBottomWithPanel && !IsAlignTopWith && !IsAlignBottomWith));

        // RPState flag checks.
        public bool IsUnresolved => _mState == RelativePanelState.Unresolved;
        public bool IsPending => (_mState & RelativePanelState.Pending) == RelativePanelState.Pending;
        public bool IsMeasured => (_mState & RelativePanelState.Measured) == RelativePanelState.Measured;
        public bool IsArrangedHorizontally => (_mState & RelativePanelState.ArrangedHorizontally) == RelativePanelState.ArrangedHorizontally;
        public bool IsArrangedVertically => (_mState & RelativePanelState.ArrangedVertically) == RelativePanelState.ArrangedVertically;
        public bool IsArranged => (_mState & RelativePanelState.Arranged) == RelativePanelState.Arranged;

        public void SetPending(bool value)
        {
            if (value)
            {
                _mState |= RelativePanelState.Pending;
            }
            else
            {
                _mState &= ~RelativePanelState.Pending;
            }
        }

        public void SetMeasured(bool value)
        {
            if (value)
            {
                _mState |= RelativePanelState.Measured;
            }
            else
            {
                _mState &= ~RelativePanelState.Measured;
            }
        }

        public void SetArrangedHorizontally(bool value)
        {
            if (value)
            {
                _mState |= RelativePanelState.ArrangedHorizontally;
            }
            else
            {
                _mState &= ~RelativePanelState.ArrangedHorizontally;
            }
        }

        public void SetArrangedVertically(bool value)
        {
            if (value)
            {
                _mState |= RelativePanelState.ArrangedVertically;
            }
            else
            {
                _mState &= ~RelativePanelState.ArrangedVertically;
            }
        }

        // RPEdge flag checks.
        public bool IsLeftOf => (MConstraints & RelativePanelConstraints.LeftOf) == RelativePanelConstraints.LeftOf;
        public bool IsAbove => (MConstraints & RelativePanelConstraints.Above) == RelativePanelConstraints.Above;
        public bool IsRightOf => (MConstraints & RelativePanelConstraints.RightOf) == RelativePanelConstraints.RightOf;
        public bool IsBelow => (MConstraints & RelativePanelConstraints.Below) == RelativePanelConstraints.Below;
        public bool IsAlignHorizontalCenterWith => (MConstraints & RelativePanelConstraints.AlignHorizontalCenterWith) == RelativePanelConstraints.AlignHorizontalCenterWith;
        public bool IsAlignVerticalCenterWith => (MConstraints & RelativePanelConstraints.AlignVerticalCenterWith) == RelativePanelConstraints.AlignVerticalCenterWith;
        public bool IsAlignLeftWith => (MConstraints & RelativePanelConstraints.AlignLeftWith) == RelativePanelConstraints.AlignLeftWith;
        public bool IsAlignTopWith => (MConstraints & RelativePanelConstraints.AlignTopWith) == RelativePanelConstraints.AlignTopWith;
        public bool IsAlignRightWith => (MConstraints & RelativePanelConstraints.AlignRightWith) == RelativePanelConstraints.AlignRightWith;
        public bool IsAlignBottomWith => (MConstraints & RelativePanelConstraints.AlignBottomWith) == RelativePanelConstraints.AlignBottomWith;
        public bool IsAlignLeftWithPanel => (MConstraints & RelativePanelConstraints.AlignLeftWithPanel) == RelativePanelConstraints.AlignLeftWithPanel;
        public bool IsAlignTopWithPanel => (MConstraints & RelativePanelConstraints.AlignTopWithPanel) == RelativePanelConstraints.AlignTopWithPanel;
        public bool IsAlignRightWithPanel => (MConstraints & RelativePanelConstraints.AlignRightWithPanel) == RelativePanelConstraints.AlignRightWithPanel;
        public bool IsAlignBottomWithPanel => (MConstraints & RelativePanelConstraints.AlignBottomWithPanel) == RelativePanelConstraints.AlignBottomWithPanel;
        public bool IsAlignHorizontalCenterWithPanel => (MConstraints & RelativePanelConstraints.AlignHorizontalCenterWithPanel) == RelativePanelConstraints.AlignHorizontalCenterWithPanel;
        public bool IsAlignVerticalCenterWithPanel => (MConstraints & RelativePanelConstraints.AlignVerticalCenterWithPanel) == RelativePanelConstraints.AlignVerticalCenterWithPanel;

        public void SetLeftOfConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MLeftOfNode = neighbor;
                MConstraints |= RelativePanelConstraints.LeftOf;
            }
            else
            {
                MLeftOfNode = null;
                MConstraints &= ~RelativePanelConstraints.LeftOf;
            }
        }
        public void SetAboveConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MAboveNode = neighbor;
                MConstraints |= RelativePanelConstraints.Above;
            }
            else
            {
                MAboveNode = null;
                MConstraints &= ~RelativePanelConstraints.Above;
            }
        }

        public void SetRightOfConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MRightOfNode = neighbor;
                MConstraints |= RelativePanelConstraints.RightOf;
            }
            else
            {
                MRightOfNode = null;
                MConstraints &= ~RelativePanelConstraints.RightOf;
            }
        }
        public void SetBelowConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MBelowNode = neighbor;
                MConstraints |= RelativePanelConstraints.Below;
            }
            else
            {
                MBelowNode = null;
                MConstraints &= ~RelativePanelConstraints.Below;
            }
        }

        public void SetAlignHorizontalCenterWithConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MAlignHorizontalCenterWithNode = neighbor;
                MConstraints |= RelativePanelConstraints.AlignHorizontalCenterWith;
            }
            else
            {
                MAlignHorizontalCenterWithNode = null;
                MConstraints &= ~RelativePanelConstraints.AlignHorizontalCenterWith;
            }
        }

        public void SetAlignVerticalCenterWithConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MAlignVerticalCenterWithNode = neighbor;
                MConstraints |= RelativePanelConstraints.AlignVerticalCenterWith;
            }
            else
            {
                MAlignVerticalCenterWithNode = null;
                MConstraints &= ~RelativePanelConstraints.AlignVerticalCenterWith;
            }
        }

        public void SetAlignLeftWithConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MAlignLeftWithNode = neighbor;
                MConstraints |= RelativePanelConstraints.AlignLeftWith;
            }
            else
            {
                MAlignLeftWithNode = null;
                MConstraints &= ~RelativePanelConstraints.AlignLeftWith;
            }
        }
        public void SetAlignTopWithConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MAlignTopWithNode = neighbor;
                MConstraints |= RelativePanelConstraints.AlignTopWith;
            }
            else
            {
                MAlignTopWithNode = null;
                MConstraints &= ~RelativePanelConstraints.AlignTopWith;
            }
        }
        public void SetAlignRightWithConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MAlignRightWithNode = neighbor;
                MConstraints |= RelativePanelConstraints.AlignRightWith;
            }
            else
            {
                MAlignRightWithNode = null;
                MConstraints &= ~RelativePanelConstraints.AlignRightWith;
            }
        }
        public void SetAlignBottomWithConstraint(RelativePanelNode? neighbor)
        {
            if (neighbor is not null)
            {
                MAlignBottomWithNode = neighbor;
                MConstraints |= RelativePanelConstraints.AlignBottomWith;
            }
            else
            {
                MAlignBottomWithNode = null;
                MConstraints &= ~RelativePanelConstraints.AlignBottomWith;
            }
        }
        public void SetAlignLeftWithPanelConstraint(bool value)
        {
            if (value)
            {
                MConstraints |= RelativePanelConstraints.AlignLeftWithPanel;
            }
            else
            {
                MConstraints &= ~RelativePanelConstraints.AlignLeftWithPanel;
            }
        }

        public void SetAlignTopWithPanelConstraint(bool value)
        {
            if (value)
            {
                MConstraints |= RelativePanelConstraints.AlignTopWithPanel;
            }
            else
            {
                MConstraints &= ~RelativePanelConstraints.AlignTopWithPanel;
            }
        }
        public void SetAlignRightWithPanelConstraint(bool value)
        {
            if (value)
            {
                MConstraints |= RelativePanelConstraints.AlignRightWithPanel;
            }
            else
            {
                MConstraints &= ~RelativePanelConstraints.AlignRightWithPanel;
            }
        }
        public void SetAlignBottomWithPanelConstraint(bool value)
        {
            if (value)
            {
                MConstraints |= RelativePanelConstraints.AlignBottomWithPanel;
            }
            else
            {
                MConstraints &= ~RelativePanelConstraints.AlignBottomWithPanel;
            }
        }
        public void SetAlignHorizontalCenterWithPanelConstraint(bool value)
        {
            if (value)
            {
                MConstraints |= RelativePanelConstraints.AlignHorizontalCenterWithPanel;
            }
            else
            {
                MConstraints &= ~RelativePanelConstraints.AlignHorizontalCenterWithPanel;
            }
        }
        public void SetAlignVerticalCenterWithPanelConstraint(bool value)
        {
            if (value)
            {
                MConstraints |= RelativePanelConstraints.AlignVerticalCenterWithPanel;
            }
            else
            {
                MConstraints &= ~RelativePanelConstraints.AlignVerticalCenterWithPanel;
            }
        }

        public void UnmarkNeighborsAsHorizontalOrVerticalLeaves()
        {
            bool isHorizontallyCenteredFromLeft = false;
            bool isHorizontallyCenteredFromRight = false;
            bool isVerticallyCenteredFromTop = false;
            bool isVerticallyCenteredFromBottom = false;

            if (!IsAlignLeftWithPanel)
            {
                if (IsAlignLeftWith)
                {
                    Debug.Assert(MAlignLeftWithNode != null);
                    MAlignLeftWithNode.MIsHorizontalLeaf = false;
                }
                else if (IsAlignHorizontalCenterWith)
                {
                    isHorizontallyCenteredFromLeft = true;
                }
                else if (IsRightOf)
                {
                    Debug.Assert(MRightOfNode != null);
                    MRightOfNode.MIsHorizontalLeaf = false;
                }
            }

            if (!IsAlignTopWithPanel)
            {
                if (IsAlignTopWith)
                {
                    Debug.Assert(MAlignTopWithNode != null);
                    MAlignTopWithNode.MIsVerticalLeaf = false;
                }
                else if (IsAlignVerticalCenterWith)
                {
                    isVerticallyCenteredFromTop = true;
                }
                else if (IsBelow)
                {
                    Debug.Assert(MBelowNode != null);
                    MBelowNode.MIsVerticalLeaf = false;
                }
            }

            if (!IsAlignRightWithPanel)
            {
                if (IsAlignRightWith)
                {
                    Debug.Assert(MAlignRightWithNode != null);
                    MAlignRightWithNode.MIsHorizontalLeaf = false;
                }
                else if (IsAlignHorizontalCenterWith)
                {
                    isHorizontallyCenteredFromRight = true;
                }
                else if (IsLeftOf)
                {
                    Debug.Assert(MLeftOfNode != null);
                    MLeftOfNode.MIsHorizontalLeaf = false;
                }
            }

            if (!IsAlignBottomWithPanel)
            {
                if (IsAlignBottomWith)
                {
                    Debug.Assert(MAlignBottomWithNode != null);
                    MAlignBottomWithNode.MIsVerticalLeaf = false;
                }
                else if (IsAlignVerticalCenterWith)
                {
                    isVerticallyCenteredFromBottom = true;
                }
                else if (IsAbove)
                {
                    Debug.Assert(MAboveNode != null);
                    MAboveNode.MIsVerticalLeaf = false;
                }
            }

            if (isHorizontallyCenteredFromLeft && isHorizontallyCenteredFromRight)
            {
                Debug.Assert(MAlignHorizontalCenterWithNode != null);
                MAlignHorizontalCenterWithNode.MIsHorizontalLeaf = false;
            }

            if (isVerticallyCenteredFromTop && isVerticallyCenteredFromBottom)
            {
                Debug.Assert(MAlignVerticalCenterWithNode != null);
                MAlignVerticalCenterWithNode.MIsVerticalLeaf = false;
            }
        }
    }

    [Flags]
    enum RelativePanelConstraints
    {
        None = 0x00000,
        LeftOf = 0x00001,
        Above = 0x00002,
        RightOf = 0x00004,
        Below = 0x00008,
        AlignHorizontalCenterWith = 0x00010,
        AlignVerticalCenterWith = 0x00020,
        AlignLeftWith = 0x00040,
        AlignTopWith = 0x00080,
        AlignRightWith = 0x00100,
        AlignBottomWith = 0x00200,
        AlignLeftWithPanel = 0x00400,
        AlignTopWithPanel = 0x00800,
        AlignRightWithPanel = 0x01000,
        AlignBottomWithPanel = 0x02000,
        AlignHorizontalCenterWithPanel = 0x04000,
        AlignVerticalCenterWithPanel = 0x08000
    }

    [Flags]
    enum RelativePanelState
    {
        Unresolved = 0x00,
        Pending = 0x01,
        Measured = 0x02,
        ArrangedHorizontally = 0x04,
        ArrangedVertically = 0x08,
        Arranged = 0x0C
    }
}