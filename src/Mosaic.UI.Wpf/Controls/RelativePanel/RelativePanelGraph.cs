namespace Mosaic.UI.Wpf.Controls
{
    internal class RelativePanelGraph
    {
        private double _mMinX = 0;
        private double _mMaxX = 0;
        private double _mMinY = 0;
        private double _mMaxY = 0;
        private bool _mIsMinCapped = false;
        private bool _mIsMaxCapped = false;
        private Size _mAvailableSizeForNodeResolution;
        private List<RelativePanelNode> _mNodes = new();
        public List<RelativePanelNode> Nodes => _mNodes;

        public void AddNodes(UIElementCollection uIElementCollection)
        {
            foreach (var item in uIElementCollection)
            {
                if (item is DependencyObject dpo)
                {
                    _mNodes.Add(new RelativePanelNode(dpo));
                }
            }
        }

        public void ResolveConstraints(DependencyObject parent)
        {
            foreach (var node in _mNodes)
            {
                object? value = node.GetLeftOfValue();
                if (value is not null)
                {
                    node.SetLeftOfConstraint(GetNodeByValue(value, parent));
                }

                value = node.GetAboveValue();
                if (value is not null)
                {
                    node.SetAboveConstraint(GetNodeByValue(value, parent));
                }

                value = node.GetRightOfValue();
                if (value is not null)
                {
                    node.SetRightOfConstraint(GetNodeByValue(value, parent));
                }

                value = node.GetBelowValue();
                if (value is not null)
                {
                    node.SetBelowConstraint(GetNodeByValue(value, parent));
                }

                value = node.GetAlignHorizontalCenterWithValue();
                if (value is not null)
                {
                    node.SetAlignHorizontalCenterWithConstraint(GetNodeByValue(value, parent));
                }

                value = node.GetAlignVerticalCenterWithValue();
                if (value is not null)
                {
                    node.SetAlignVerticalCenterWithConstraint(GetNodeByValue(value, parent));
                }

                value = node.GetAlignLeftWithValue();
                if (value is not null)
                {
                    node.SetAlignLeftWithConstraint(GetNodeByValue(value, parent));
                }

                value = node.GetAlignTopWithValue();
                if (value is not null)
                {
                    node.SetAlignTopWithConstraint(GetNodeByValue(value, parent));
                }

                value = node.GetAlignRightWithValue();
                if (value is not null)
                {
                    node.SetAlignRightWithConstraint(GetNodeByValue(value, parent));
                }

                value = node.GetAlignBottomWithValue();
                if (value is not null)
                {
                    node.SetAlignBottomWithConstraint(GetNodeByValue(value, parent));
                }

                node.SetAlignLeftWithPanelConstraint(node.GetAlignLeftWithPanelValue());
                node.SetAlignTopWithPanelConstraint(node.GetAlignTopWithPanelValue());
                node.SetAlignRightWithPanelConstraint(node.GetAlignRightWithPanelValue());
                node.SetAlignBottomWithPanelConstraint(node.GetAlignBottomWithPanelValue());
                node.SetAlignHorizontalCenterWithPanelConstraint(node.GetAlignHorizontalCenterWithPanelValue());
                node.SetAlignVerticalCenterWithPanelConstraint(node.GetAlignVerticalCenterWithPanelValue());
            }
        }

        public void MeasureNodes(Size availableSize)
        {
            foreach (var node in Nodes)
            {
                MeasureNode(node, availableSize);
            }
            _mAvailableSizeForNodeResolution = availableSize;
        }

        public void ArrangeNodes(Rect finalRect)
        {
            Size finalSize = finalRect.Size;

            // If the final size is the same as the available size that we used
            // to measure the nodes, this means that the pseudo-arrange pass  
            // that we did during the measure pass is, in fact, valid and the 
            // ArrangeRects that were calculated for each node are correct. In 
            // other words, we can just go ahead and call arrange on each
            // element. However, if the width and/or height of the final size
            // differs (e.g. when the element's HorizontalAlignment and/or
            // VerticalAlignment is something other than Stretch and thus the final
            // size corresponds to the desired size of the panel), we must first
            // recalculate the horizontal and/or vertical values of the ArrangeRects,
            // respectively.
            if (_mAvailableSizeForNodeResolution.Width != finalSize.Width)
            {
                foreach (RelativePanelNode node in _mNodes)
                {
                    node.SetArrangedHorizontally(false);
                }
            }

            if (_mAvailableSizeForNodeResolution.Height != finalSize.Height)
            {
                foreach (RelativePanelNode node in _mNodes)
                {
                    node.SetArrangedVertically(false);
                }
            }

            _mAvailableSizeForNodeResolution = finalSize;

            foreach (RelativePanelNode node in _mNodes)
            {
                Rect layoutSlot = new Rect(
                 x: Math.Max(node.MArrangeRect.X + finalRect.X, 0.0),
                 y: Math.Max(node.MArrangeRect.Y + finalRect.Y, 0.0),
                 width: Math.Max(node.MArrangeRect.Width, 0.0),
                 height: Math.Max(node.MArrangeRect.Height, 0.0));

                node.Arrange(layoutSlot);
            }
        }

        /// <summary>
        /// Starting at zero, we will traverse all the horizontal and vertical 
        /// chains in the graph one by one while moving a "cursor" up and down 
        /// as we go in jumps that are equivalent to the size of the element 
        /// associated to the node that we are currently visiting. Everytime
        /// that the cursor hits a new max or new min value, we cache it. At the
        /// end, the difference between the max and the min values corresponds
        /// to the desired size of that chain. The size of the biggest chain
        /// also corresponds to the desired size of the panel.
        /// </summary>
        /// <returns></returns>
        public Size CalculateDesiredSize()
        {
            Size maxDesiredSize = new Size(0, 0);

            MarkHorizontalAndVerticalLeaves();

            foreach (RelativePanelNode node in _mNodes)
            {
                if (node.MIsHorizontalLeaf)
                {
                    _mMinX = 0.0f;
                    _mMaxX = 0.0f;
                    _mIsMinCapped = false;
                    _mIsMaxCapped = false;

                    AccumulatePositiveDesiredWidth(node, 0.0f);
                    maxDesiredSize.Width = Math.Max(maxDesiredSize.Width, _mMaxX - _mMinX);
                }

                if (node.MIsVerticalLeaf)
                {
                    _mMinY = 0.0f;
                    _mMaxY = 0.0f;
                    _mIsMinCapped = false;
                    _mIsMaxCapped = false;

                    AccumulatePositiveDesiredHeight(node, 0.0f);
                    maxDesiredSize.Height = Math.Max(maxDesiredSize.Height, _mMaxY - _mMinY);
                }
            }

            return maxDesiredSize;
        }

        private RelativePanelNode GetNodeByValue(object value, DependencyObject parent)
        {
            if (value is UIElement element)
            {
                foreach (var node in _mNodes)
                {
                    if (node.Element == element)
                    {
                        return node;
                    }
                }
            }
            throw new InvalidOperationException($"Element {value} not found in RelativePanel"); //TODO: Better error here
        }

        // Starting off with the space that is available to the entire panel
        // (a.k.a. available size), we will constrain this space little by 
        // little based on the ArrangeRects of the dependencies that the
        // node has. The end result corresponds to the MeasureRect of this node. 
        // Consider the following example: if an element is to the left of a 
        // sibling, that means that the space that is available to this element
        // in particular is now the available size minus the Width of the 
        // ArrangeRect associated with this sibling.
        void CalculateMeasureRectHorizontally(RelativePanelNode node, Size availableSize, out double x, out double width)
        {

            bool isHorizontallyCenteredFromLeft = false;
            bool isHorizontallyCenteredFromRight = false;

            // The initial values correspond to the entire available space. In
            // other words, the edges of the element are aligned to the edges
            // of the panel by default. We will now constrain each side of this
            // space as necessary.
            x = 0.0f;
            width = availableSize.Width;

            // If we have infinite available width, then the Width of the
            // MeasureRect is also infinite; we do not have to constrain it.
            if (!double.IsPositiveInfinity(availableSize.Width))
            {
                // Constrain the left side of the available space, i.e.
                // a.) The child has its left edge aligned with the panel (default),
                // b.) The child has its left edge aligned with the left edge of a sibling,
                // or c.) The child is positioned to the right of a sibling.
                //
                //  |;;                 |               |                                                   
                //  |;;                 |               |                
                //  |;;                 |.......:|                       ;;......:;; 
                //  |;;                 |;             ;|       .               ;;             ;;
                //  |;;                 |;             ;|     .;;............   ;;             ;;
                //  |;;                 |;             ;|   .;;;;......   ;;             ;;
                //  |;;                 |;             ;|    ':;;......   ;;             ;;
                //  |;;                 |;             ;|      ':               ;;             ;;       
                //  |;;                 |.......:|                       ........:
                //  |;;                 |               |               
                //  |;;                 |               |
                //  AlignLeftWithPanel  AlignLeftWith   RightOf
                //
                if (!node.IsAlignLeftWithPanel)
                {
                    if (node.IsAlignLeftWith)
                    {
                        Debug.Assert(node.MAlignLeftWithNode != null);
                        RelativePanelNode alignLeftWithNeighbor = node.MAlignLeftWithNode;
                        double restrictedHorizontalSpace = alignLeftWithNeighbor.MArrangeRect.X;

                        x = restrictedHorizontalSpace;
                        width -= restrictedHorizontalSpace;
                    }
                    else if (node.IsAlignHorizontalCenterWith)
                    {
                        isHorizontallyCenteredFromLeft = true;
                    }
                    else if (node.IsRightOf)
                    {
                        Debug.Assert(node.MRightOfNode != null);
                        RelativePanelNode rightOfNeighbor = node.MRightOfNode;
                        double restrictedHorizontalSpace = rightOfNeighbor.MArrangeRect.X + rightOfNeighbor.MArrangeRect.Width;

                        x = restrictedHorizontalSpace;
                        width -= restrictedHorizontalSpace;
                    }
                }

                // Constrain the right side of the available space, i.e.
                // a) The child has its right edge aligned with the panel (default),
                // b) The child has its right edge aligned with the right edge of a sibling,
                // or c) The child is positioned to the left of a sibling.
                //  
                //                                          |               |                   ;;|
                //                                          |               |                   ;;|
                //  ;;......:;;                       |;......:;|                   ;;|
                //  ;;             ;;               .       |;             ;|                   ;;|
                //  ;;             ;;   ............;;.     |;             ;|                   ;;|
                //  ;;             ;;   ......;;;;.   |;             ;|                   ;;|
                //  ;;             ;;   ......;;:'    |;             ;|                   ;;|
                //  ;;             ;;               :'      |;             ;|                   ;;|
                //  ........:                       |.......:|                   ;;|
                //                                          |               |                   ;;|
                //                                          |               |                   ;;|
                //                                          LeftOf          AlignRightWith      AlignRightWithPanel
                //
                if (!node.IsAlignRightWithPanel)
                {
                    if (node.IsAlignRightWith)
                    {
                        Debug.Assert(node.MAlignRightWithNode != null);
                        RelativePanelNode alignRightWithNeighbor = node.MAlignRightWithNode;
                        width -= availableSize.Width - (alignRightWithNeighbor.MArrangeRect.X + alignRightWithNeighbor.MArrangeRect.Width);
                    }
                    else if (node.IsAlignHorizontalCenterWith)
                    {
                        isHorizontallyCenteredFromRight = true;
                    }
                    else if (node.IsLeftOf)
                    {
                        Debug.Assert(node.MLeftOfNode != null);
                        RelativePanelNode leftOfNeighbor = node.MLeftOfNode;
                        width -= availableSize.Width - leftOfNeighbor.MArrangeRect.X;
                    }
                }

                if (isHorizontallyCenteredFromLeft && isHorizontallyCenteredFromRight)
                {
                    Debug.Assert(node.MAlignHorizontalCenterWithNode != null);
                    RelativePanelNode alignHorizontalCenterWithNeighbor = node.MAlignHorizontalCenterWithNode;
                    double centerOfNeighbor = alignHorizontalCenterWithNeighbor.MArrangeRect.X + (alignHorizontalCenterWithNeighbor.MArrangeRect.Width / 2.0f);
                    width = Math.Min(centerOfNeighbor, availableSize.Width - centerOfNeighbor) * 2.0f;
                    x = centerOfNeighbor - (width / 2.0f);
                }
            }
        }
        void CalculateMeasureRectVertically(RelativePanelNode node, Size availableSize, out double y, out double height)
        {

            bool isVerticallyCenteredFromTop = false;
            bool isVerticallyCenteredFromBottom = false;

            // The initial values correspond to the entire available space. In
            // other words, the edges of the element are aligned to the edges
            // of the panel by default. We will now constrain each side of this
            // space as necessary.
            y = 0.0f;
            height = availableSize.Height;

            // If we have infinite available height, then the Height of the
            // MeasureRect is also infinite; we do not have to constrain it.
            if (!double.IsPositiveInfinity(availableSize.Height))
            {
                // Constrain the top of the available space, i.e.
                // a) The child has its top edge aligned with the panel (default),
                // b) The child has its top edge aligned with the top edge of a sibling,
                // or c) The child is positioned to the below a sibling.
                //
                //  ================================== AlignTopWithPanel
                //  .................
                //
                //
                //
                //  --------;;=============;;--------- AlignTopWith
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //  --------.=============.--------- Below 
                //                  .
                //                .:;:.
                //              .:;;;;;:.
                //                ;;;;;
                //                ;;;;;
                //                ;;;;;
                //                ;;;;;
                //                ;;;;;
                //
                //          ;;......:;; 
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ........:
                //
                if (!node.IsAlignTopWithPanel)
                {
                    if (node.IsAlignTopWith)
                    {
                        Debug.Assert(node.MAlignTopWithNode != null);
                        RelativePanelNode alignTopWithNeighbor = node.MAlignTopWithNode;
                        double restrictedVerticalSpace = alignTopWithNeighbor.MArrangeRect.Y;

                        y = restrictedVerticalSpace;
                        height -= restrictedVerticalSpace;
                    }
                    else if (node.IsAlignVerticalCenterWith)
                    {
                        isVerticallyCenteredFromTop = true;
                    }
                    else if (node.IsBelow)
                    {
                        Debug.Assert(node.MBelowNode != null);
                        RelativePanelNode belowNeighbor = node.MBelowNode;
                        double restrictedVerticalSpace = belowNeighbor.MArrangeRect.Y + belowNeighbor.MArrangeRect.Height;

                        y = restrictedVerticalSpace;
                        height -= restrictedVerticalSpace;
                    }
                }

                // Constrain the bottom of the available space, i.e.
                // a.) The child has its bottom edge aligned with the panel (default),
                // b.) The child has its bottom edge aligned with the bottom edge of a sibling,
                // or c.) The child is positioned to the above a sibling.
                //
                //          ;;......:;; 
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ........:
                //
                //                ;;;;;
                //                ;;;;;
                //                ;;;;;
                //                ;;;;;
                //                ;;;;;
                //              ..;;;;;..
                //               '..:'
                //                 ':`
                //                  
                //  --------;;=============;;--------- Above 
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //          ;;             ;;
                //  --------.=============.--------- AlignBottomWith
                //
                // 
                //
                //  .................
                //  ================================== AlignBottomWithPanel
                //
                if (!node.IsAlignBottomWithPanel)
                {
                    if (node.IsAlignBottomWith)
                    {
                        Debug.Assert(node.MAlignBottomWithNode != null);
                        RelativePanelNode alignBottomWithNeighbor = node.MAlignBottomWithNode;
                        height -= availableSize.Height - (alignBottomWithNeighbor.MArrangeRect.Y + alignBottomWithNeighbor.MArrangeRect.Height);
                    }
                    else if (node.IsAlignVerticalCenterWith)
                    {
                        isVerticallyCenteredFromBottom = true;
                    }
                    else if (node.IsAbove)
                    {
                        Debug.Assert(node.MAboveNode != null);
                        RelativePanelNode aboveNeighbor = node.MAboveNode;
                        height -= availableSize.Height - aboveNeighbor.MArrangeRect.Y;
                    }
                }

                if (isVerticallyCenteredFromTop && isVerticallyCenteredFromBottom)
                {
                    Debug.Assert(node.MAlignVerticalCenterWithNode != null);
                    RelativePanelNode alignVerticalCenterWithNeighbor = node.MAlignVerticalCenterWithNode;
                    double centerOfNeighbor = alignVerticalCenterWithNeighbor.MArrangeRect.Y + (alignVerticalCenterWithNeighbor.MArrangeRect.Height / 2.0f);
                    height = Math.Min(centerOfNeighbor, availableSize.Height - centerOfNeighbor) * 2.0f;
                    y = centerOfNeighbor - (height / 2.0f);
                }
            }
        }

        // The ArrangeRect (a.k.a. layout slot) corresponds to the specific rect 
        // within the MeasureRect that will be given to an element for it to
        // position itself. The exact rect is calculated based on anchoring
        // and, unless anchoring dictates otherwise, the size of the
        // ArrangeRect is equal to the desired size of the element itself. 
        // Consider the following example: if the node is right-anchored, the 
        // right side of the ArrangeRect should overlap with the right side
        // of the MeasureRect; this same logic is applied to the other three
        // sides of the rect.
        void CalculateArrangeRectHorizontally(RelativePanelNode node, out double x, out double width)
        {
            Rect measureRect = node.MMeasureRect;
            double desiredWidth = Math.Min(measureRect.Width, node.DesiredWidth);

            Debug.Assert(node.IsMeasured && !double.IsPositiveInfinity(measureRect.Width));

            // The initial values correspond to the left corner, using the 
            // desired size of element. If no attached properties were set, 
            // this means that the element will default to the left corner of
            // the panel.
            x = measureRect.X;
            width = desiredWidth;

            if (node.IsLeftAnchored)
            {
                if (node.IsRightAnchored)
                {
                    x = measureRect.X;
                    width = measureRect.Width;
                }
                else
                {
                    x = measureRect.X;
                    width = desiredWidth;
                }
            }
            else if (node.IsRightAnchored)
            {
                x = measureRect.X + measureRect.Width - desiredWidth;
                width = desiredWidth;
            }
            else if (node.IsHorizontalCenterAnchored)
            {
                x = measureRect.X + (measureRect.Width / 2.0f) - (desiredWidth / 2.0f);
                width = desiredWidth;
            }
        }
        void CalculateArrangeRectVertically(RelativePanelNode node, out double y, out double height)
        {

            Rect measureRect = node.MMeasureRect;
            double desiredHeight = Math.Min(measureRect.Height, node.DesiredHeight);

            Debug.Assert(node.IsMeasured && !double.IsPositiveInfinity(measureRect.Height));

            // The initial values correspond to the top corner, using the 
            // desired size of element. If no attached properties were set, 
            // this means that the element will default to the top corner of
            // the panel.
            y = measureRect.Y;
            height = desiredHeight;

            if (node.IsTopAnchored)
            {
                if (node.IsBottomAnchored)
                {
                    y = measureRect.Y;
                    height = measureRect.Height;
                }
                else
                {
                    y = measureRect.Y;
                    height = desiredHeight;
                }
            }
            else if (node.IsBottomAnchored)
            {
                y = measureRect.Y + measureRect.Height - desiredHeight;
                height = desiredHeight;
            }
            else if (node.IsVerticalCenterAnchored)
            {
                y = measureRect.Y + (measureRect.Height / 2.0f) - (desiredHeight / 2.0f);
                height = desiredHeight;
            }
        }

        void MarkHorizontalAndVerticalLeaves()
        {
            foreach (var node in _mNodes)
            {
                node.MIsHorizontalLeaf = true;
                node.MIsVerticalLeaf = true;
            }

            foreach (var node in _mNodes)
            {
                node.UnmarkNeighborsAsHorizontalOrVerticalLeaves();
            }
        }

        void AccumulatePositiveDesiredWidth(RelativePanelNode node, double x)
        {
            double initialX = x;
            bool isHorizontallyCenteredFromLeft = false;
            bool isHorizontallyCenteredFromRight = false;

            Debug.Assert(node.IsMeasured);

            // If we are going in the positive direction, move the cursor
            // right by the desired width of the node with which we are 
            // currently working and refresh the maximum positive value.
            x += node.DesiredWidth;
            _mMaxX = Math.Max(_mMaxX, x);

            if (node.IsAlignLeftWithPanel)
            {
                if (!_mIsMaxCapped)
                {
                    _mMaxX = x;
                    _mIsMaxCapped = true;
                }
            }
            else if (node.IsAlignLeftWith)
            {
                // If the AlignLeftWithNode and AlignRightWithNode are the
                // same element, we can skip the former, since we will move 
                // through the latter later.
                if (node.MAlignLeftWithNode != node.MAlignRightWithNode)
                {
                    Debug.Assert(node.MAlignLeftWithNode != null);
                    AccumulateNegativeDesiredWidth(node.MAlignLeftWithNode, x);
                }
            }
            else if (node.IsAlignHorizontalCenterWith)
            {
                isHorizontallyCenteredFromLeft = true;
            }
            else if (node.IsRightOf)
            {
                Debug.Assert(node.MRightOfNode != null);
                AccumulatePositiveDesiredWidth(node.MRightOfNode, x);
            }

            if (node.IsAlignRightWithPanel)
            {
                if (_mIsMinCapped)
                {
                    _mMinX = Math.Min(_mMinX, initialX);
                }
                else
                {
                    _mMinX = initialX;
                    _mIsMinCapped = true;
                }
            }
            else if (node.IsAlignRightWith)
            {
                // If this element's right is aligned to some other 
                // element's right, now we will be going in the positive
                // direction to that other element in order to continue the
                // traversal of the dependency chain. But first, since we 
                // arrived to the node where we currently are by going in
                // the positive direction, that means that we have already 
                // moved the cursor right to calculate the maximum positive 
                // value, so we will use the initial value of Y.
                Debug.Assert(node.MAlignRightWithNode != null);
                AccumulatePositiveDesiredWidth(node.MAlignRightWithNode, initialX);
            }
            else if (node.IsAlignHorizontalCenterWith)
            {
                isHorizontallyCenteredFromRight = true;
            }
            else if (node.IsLeftOf)
            {
                // If this element is to the left of some other element,
                // now we will be going in the negative direction to that
                // other element in order to continue the traversal of the
                // dependency chain. But first, since we arrived to the
                // node where we currently are by going in the positive
                // direction, that means that we have already moved the 
                // cursor right to calculate the maximum positive value, so
                // we will use the initial value of X.
                Debug.Assert(node.MLeftOfNode != null);
                AccumulateNegativeDesiredWidth(node.MLeftOfNode, initialX);
            }

            if (isHorizontallyCenteredFromLeft && isHorizontallyCenteredFromRight)
            {
                Debug.Assert(node.MAlignHorizontalCenterWithNode != null);
                double centerX = x - (node.DesiredWidth / 2.0);
                double edgeX = centerX - (node.MAlignHorizontalCenterWithNode.DesiredWidth / 2.0);
                _mMinX = Math.Min(_mMinX, edgeX);
                AccumulatePositiveDesiredWidth(node.MAlignHorizontalCenterWithNode, edgeX);
            }
            else if (node.IsHorizontalCenterAnchored)
            {
                // If this node is horizontally anchored to the center, then it
                // means that it is the root of this dependency chain based on
                // the current definition of precedence for constraints: 
                // e.g. AlignLeftWithPanel 
                // > AlignLeftWith 
                // > RightOf
                // > AlignHorizontalCenterWithPanel    
                // Thus, we can report its width as twice the width of 
                // either the difference from center to left or the difference
                // from center to right, whichever is the greatest.
                double centerX = x - (node.DesiredWidth / 2.0);
                double upper = _mMaxX - centerX;
                double lower = centerX - _mMinX;
                _mMaxX = Math.Max(upper, lower) * 2.0;
                _mMinX = 0.0;
            }
        }
        void AccumulateNegativeDesiredWidth(RelativePanelNode node, double x)
        {

            double initialX = x;
            bool isHorizontallyCenteredFromLeft = false;
            bool isHorizontallyCenteredFromRight = false;

            Debug.Assert(node.IsMeasured);

            // If we are going in the negative direction, move the cursor
            // left by the desired width of the node with which we are 
            // currently working and refresh the minimum negative value.
            x -= node.DesiredWidth;
            _mMinX = Math.Min(_mMinX, x);

            if (node.IsAlignRightWithPanel)
            {
                if (!_mIsMinCapped)
                {
                    _mMinX = x;
                    _mIsMinCapped = true;
                }
            }
            else if (node.IsAlignRightWith)
            {
                // If the AlignRightWithNode and AlignLeftWithNode are the
                // same element, we can skip the former, since we will move 
                // through the latter later.
                if (node.MAlignRightWithNode != node.MAlignLeftWithNode)
                {
                    Debug.Assert(node.MAlignRightWithNode != null);
                    AccumulatePositiveDesiredWidth(node.MAlignRightWithNode, x);
                }
            }
            else if (node.IsAlignHorizontalCenterWith)
            {
                isHorizontallyCenteredFromRight = true;
            }
            else if (node.IsLeftOf)
            {
                Debug.Assert(node.MLeftOfNode != null);
                AccumulateNegativeDesiredWidth(node.MLeftOfNode, x);
            }

            if (node.IsAlignLeftWithPanel)
            {
                if (_mIsMaxCapped)
                {
                    _mMaxX = Math.Max(_mMaxX, initialX);
                }
                else
                {
                    _mMaxX = initialX;
                    _mIsMaxCapped = true;
                }
            }
            else if (node.IsAlignLeftWith)
            {
                // If this element's left is aligned to some other element's
                // left, now we will be going in the negative direction to 
                // that other element in order to continue the traversal of
                // the dependency chain. But first, since we arrived to the
                // node where we currently are by going in the negative 
                // direction, that means that we have already moved the 
                // cursor left to calculate the minimum negative value,
                // so we will use the initial value of X.
                Debug.Assert(node.MAlignLeftWithNode != null);
                AccumulateNegativeDesiredWidth(node.MAlignLeftWithNode, initialX);
            }
            else if (node.IsAlignHorizontalCenterWith)
            {
                isHorizontallyCenteredFromLeft = true;
            }
            else if (node.IsRightOf)
            {
                // If this element is to the right of some other element,
                // now we will be going in the positive direction to that
                // other element in order to continue the traversal of the
                // dependency chain. But first, since we arrived to the
                // node where we currently are by going in the negative
                // direction, that means that we have already moved the 
                // cursor left to calculate the minimum negative value, so
                // we will use the initial value of X.
                Debug.Assert(node.MRightOfNode != null);
                AccumulatePositiveDesiredWidth(node.MRightOfNode, initialX);
            }

            if (isHorizontallyCenteredFromLeft && isHorizontallyCenteredFromRight)
            {
                Debug.Assert(node.MAlignHorizontalCenterWithNode != null);
                double centerX = x + (node.DesiredWidth / 2.0);
                double edgeX = centerX + (node.MAlignHorizontalCenterWithNode.DesiredWidth / 2.0);
                _mMaxX = Math.Max(_mMaxX, edgeX);
                AccumulateNegativeDesiredWidth(node.MAlignHorizontalCenterWithNode, edgeX);
            }
            else if (node.IsHorizontalCenterAnchored)
            {
                // If this node is horizontally anchored to the center, then it
                // means that it is the root of this dependency chain based on
                // the current definition of precedence for constraints: 
                // e.g. AlignLeftWithPanel 
                // > AlignLeftWith 
                // > RightOf
                // > AlignHorizontalCenterWithPanel    
                // Thus, we can report its width as twice the width of 
                // either the difference from center to left or the difference
                // from center to right, whichever is the greatest.
                double centerX = x + (node.DesiredWidth / 2.0);
                double upper = _mMaxX - centerX;
                double lower = centerX - _mMinX;
                _mMaxX = Math.Max(upper, lower) * 2.0f;
                _mMinX = 0.0f;
            }
        }
        void AccumulatePositiveDesiredHeight(RelativePanelNode node, double y)
        {

            double initialY = y;
            bool isVerticallyCenteredFromTop = false;
            bool isVerticallyCenteredFromBottom = false;

            Debug.Assert(node.IsMeasured);

            // If we are going in the positive direction, move the cursor
            // up by the desired height of the node with which we are 
            // currently working and refresh the maximum positive value.
            y += node.DesiredHeight;
            _mMaxY = Math.Max(_mMaxY, y);

            if (node.IsAlignTopWithPanel)
            {
                if (!_mIsMaxCapped)
                {
                    _mMaxY = y;
                    _mIsMaxCapped = true;
                }
            }
            else if (node.IsAlignTopWith)
            {
                // If the AlignTopWithNode and AlignBottomWithNode are the
                // same element, we can skip the former, since we will move 
                // through the latter later.
                if (node.MAlignTopWithNode != node.MAlignBottomWithNode)
                {
                    Debug.Assert(node.MAlignTopWithNode != null);
                    AccumulateNegativeDesiredHeight(node.MAlignTopWithNode, y);
                }
            }
            else if (node.IsAlignVerticalCenterWith)
            {
                isVerticallyCenteredFromTop = true;
            }
            else if (node.IsBelow)
            {
                Debug.Assert(node.MBelowNode != null);
                AccumulatePositiveDesiredHeight(node.MBelowNode, y);
            }

            if (node.IsAlignBottomWithPanel)
            {
                if (_mIsMinCapped)
                {
                    _mMinY = Math.Min(_mMinY, initialY);
                }
                else
                {
                    _mMinY = initialY;
                    _mIsMinCapped = true;
                }
            }
            else if (node.IsAlignBottomWith)
            {
                // If this element's bottom is aligned to some other 
                // element's bottom, now we will be going in the positive
                // direction to that other element in order to continue the
                // traversal of the dependency chain. But first, since we 
                // arrived to the node where we currently are by going in
                // the positive direction, that means that we have already 
                // moved the cursor up to calculate the maximum positive 
                // value, so we will use the initial value of Y.
                Debug.Assert(node.MAlignBottomWithNode != null);
                AccumulatePositiveDesiredHeight(node.MAlignBottomWithNode, initialY);
            }
            else if (node.IsAlignVerticalCenterWith)
            {
                isVerticallyCenteredFromBottom = true;
            }
            else if (node.IsAbove)
            {
                // If this element is above some other element, now we will 
                // be going in the negative direction to that other element
                // in order to continue the traversal of the dependency  
                // chain. But first, since we arrived to the node where we 
                // currently are by going in the positive direction, that
                // means that we have already moved the cursor up to 
                // calculate the maximum positive value, so we will use
                // the initial value of Y.
                Debug.Assert(node.MAboveNode != null);
                AccumulateNegativeDesiredHeight(node.MAboveNode, initialY);
            }

            if (isVerticallyCenteredFromTop && isVerticallyCenteredFromBottom)
            {
                Debug.Assert(node.MAlignVerticalCenterWithNode != null);
                double centerY = y - (node.DesiredHeight / 2.0);
                double edgeY = centerY - (node.MAlignVerticalCenterWithNode.DesiredHeight / 2.0);
                _mMinY = Math.Min(_mMinY, edgeY);
                AccumulatePositiveDesiredHeight(node.MAlignVerticalCenterWithNode, edgeY);
            }
            else if (node.IsVerticalCenterAnchored)
            {
                // If this node is vertically anchored to the center, then it
                // means that it is the root of this dependency chain based on
                // the current definition of precedence for constraints: 
                // e.g. AlignTopWithPanel 
                // > AlignTopWith
                // > Below
                // > AlignVerticalCenterWithPanel 
                // Thus, we can report its height as twice the height of 
                // either the difference from center to top or the difference
                // from center to bottom, whichever is the greatest.
                double centerY = y - (node.DesiredHeight / 2.0);
                double upper = _mMaxY - centerY;
                double lower = centerY - _mMinY;
                _mMaxY = Math.Max(upper, lower) * 2.0f;
                _mMinY = 0.0f;
            }
        }
        void AccumulateNegativeDesiredHeight(RelativePanelNode node, double y)
        {
            double initialY = y;
            bool isVerticallyCenteredFromTop = false;
            bool isVerticallyCenteredFromBottom = false;

            Debug.Assert(node.IsMeasured);

            // If we are going in the negative direction, move the cursor
            // down by the desired height of the node with which we are 
            // currently working and refresh the minimum negative value.
            y -= node.DesiredHeight;
            _mMinY = Math.Min(_mMinY, y);

            if (node.IsAlignBottomWithPanel)
            {
                if (!_mIsMinCapped)
                {
                    _mMinY = y;
                    _mIsMinCapped = true;
                }
            }
            else if (node.IsAlignBottomWith)
            {
                // If the AlignBottomWithNode and AlignTopWithNode are the
                // same element, we can skip the former, since we will move 
                // through the latter later.
                if (node.MAlignBottomWithNode != node.MAlignTopWithNode)
                {
                    Debug.Assert(node.MAlignBottomWithNode != null);
                    AccumulatePositiveDesiredHeight(node.MAlignBottomWithNode, y);
                }
            }
            else if (node.IsAlignVerticalCenterWith)
            {
                isVerticallyCenteredFromBottom = true;
            }
            else if (node.IsAbove)
            {
                Debug.Assert(node.MAboveNode != null);
                AccumulateNegativeDesiredHeight(node.MAboveNode, y);
            }

            if (node.IsAlignTopWithPanel)
            {
                if (_mIsMaxCapped)
                {
                    _mMaxY = Math.Max(_mMaxY, initialY);
                }
                else
                {
                    _mMaxY = initialY;
                    _mIsMaxCapped = true;
                }
            }
            else if (node.IsAlignTopWith)
            {
                // If this element's top is aligned to some other element's
                // top, now we will be going in the negative direction to 
                // that other element in order to continue the traversal of
                // the dependency chain. But first, since we arrived to the
                // node where we currently are by going in the negative 
                // direction, that means that we have already moved the 
                // cursor down to calculate the minimum negative value,
                // so we will use the initial value of Y.
                Debug.Assert(node.MAlignTopWithNode != null);
                AccumulateNegativeDesiredHeight(node.MAlignTopWithNode, initialY);
            }
            else if (node.IsAlignVerticalCenterWith)
            {
                isVerticallyCenteredFromTop = true;
            }
            else if (node.IsBelow)
            {
                // If this element is below some other element, now we'll
                // be going in the positive direction to that other element  
                // in order to continue the traversal of the dependency
                // chain. But first, since we arrived to the node where we
                // currently are by going in the negative direction, that
                // means that we have already moved the cursor down to
                // calculate the minimum negative value, so we will use
                // the initial value of Y.
                Debug.Assert(node.MBelowNode != null);
                AccumulatePositiveDesiredHeight(node.MBelowNode, initialY);
            }

            if (isVerticallyCenteredFromTop && isVerticallyCenteredFromBottom)
            {
                Debug.Assert(node.MAlignVerticalCenterWithNode != null);
                double centerY = y + (node.DesiredHeight / 2.0);
                double edgeY = centerY + (node.MAlignVerticalCenterWithNode.DesiredHeight / 2.0);
                _mMaxY = Math.Max(_mMaxY, edgeY);
                AccumulateNegativeDesiredHeight(node.MAlignVerticalCenterWithNode, edgeY);
            }
            else if (node.IsVerticalCenterAnchored)
            {
                // If this node is vertically anchored to the center, then it
                // means that it is the root of this dependency chain based on
                // the current definition of precedence for constraints: 
                // e.g. AlignTopWithPanel 
                // > AlignTopWith
                // > Below
                // > AlignVerticalCenterWithPanel 
                // Thus, we can report its height as twice the height of 
                // either the difference from center to top or the difference
                // from center to bottom, whichever is the greatest.
                double centerY = y + (node.DesiredHeight / 2.0);
                double upper = _mMaxY - centerY;
                double lower = centerY - _mMinY;
                _mMaxY = Math.Max(upper, lower) * 2.0f;
                _mMinY = 0.0f;
            }
        }

        // Calculates the MeasureRect of a node and then calls Measure on the
        // corresponding element by passing the Width and Height of this rect.
        // Given that the calculation of the MeasureRect requires the 
        // ArrangeRects of the dependencies, we call this method recursively on
        // said dependencies first and calculate both rects as we go. In other
        // words, this method is figuratively a combination of a measure pass 
        // plus a pseudo-arrange pass.
        void MeasureNode(RelativePanelNode? node, Size availableSize)
        {
            if (node is null)
            {
                return;
            }

            if (node.IsPending)
            {
                // If the node is already in the process of being resolved
                // but we tried to resolve it again, that means we are in the
                // middle of circular dependency and we must throw an 
                // InvalidOperationException. We will fail fast here and let
                // the CRelativePanel handle the rest.
                throw new InvalidOperationException("RelativePanel error: Circular dependency detected. Layout could not complete");
            }
            else if (node.IsUnresolved)
            {
                Size constrainedAvailableSize;

                // We must resolve the dependencies of this node first.
                // In the meantime, we will mark the state as pending.
                node.SetPending(true);

                MeasureNode(node.MLeftOfNode, availableSize);
                MeasureNode(node.MAboveNode, availableSize);
                MeasureNode(node.MRightOfNode, availableSize);
                MeasureNode(node.MBelowNode, availableSize);
                MeasureNode(node.MAlignLeftWithNode, availableSize);
                MeasureNode(node.MAlignTopWithNode, availableSize);
                MeasureNode(node.MAlignRightWithNode, availableSize);
                MeasureNode(node.MAlignBottomWithNode, availableSize);
                MeasureNode(node.MAlignHorizontalCenterWithNode, availableSize);
                MeasureNode(node.MAlignVerticalCenterWithNode, availableSize);

                node.SetPending(false);

                CalculateMeasureRectHorizontally(node, availableSize, out double x, out double width);
                node.MMeasureRect.X = x;
                node.MMeasureRect.Width = width;
                CalculateMeasureRectVertically(node, availableSize, out double y, out double height);
                node.MMeasureRect.Y = y;
                node.MMeasureRect.Height = height;

                constrainedAvailableSize = new Size(
                    width: Math.Max(node.MMeasureRect.Width, 0.0f),
                    height: Math.Max(node.MMeasureRect.Height, 0.0f));
                node.Measure(constrainedAvailableSize);
                node.SetMeasured(true);

                // (Pseudo-) Arranging against infinity does not make sense, so 
                // we will skip the calculations of the ArrangeRects if 
                // necessary. During the true arrange pass, we will be given a
                // non-infinite final size; we will do the necessary
                // calculations until then.
                if (!double.IsPositiveInfinity(availableSize.Width))
                {
                    CalculateArrangeRectHorizontally(node, out x, out width);
                    node.MArrangeRect.X = x;
                    node.MArrangeRect.Width = width;
                    node.SetArrangedHorizontally(true);
                }

                if (!double.IsPositiveInfinity(availableSize.Height))
                {
                    CalculateArrangeRectVertically(node, out y, out height);
                    node.MArrangeRect.Y = y;
                    node.MArrangeRect.Height = height;
                    node.SetArrangedVertically(true);
                }
            }
        }
    }
}
