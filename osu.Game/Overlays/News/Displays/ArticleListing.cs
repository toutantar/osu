﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osuTK;

namespace osu.Game.Overlays.News.Displays
{
    /// <summary>
    /// Lists articles in a vertical flow for a specified year.
    /// </summary>
    public class ArticleListing : CompositeDrawable
    {
        public Action<APINewsSidebar> SidebarMetadataUpdated;

        [Resolved]
        private IAPIProvider api { get; set; }

        private FillFlowContainer content;
        private ShowMoreButton showMore;

        private GetNewsRequest request;
        private Cursor lastCursor;

        private readonly int? year;

        /// <summary>
        /// Instantiate a listing for the specified year.
        /// </summary>
        /// <param name="year">The year to load articles from. If null, will show the most recent articles.</param>
        public ArticleListing(int? year = null)
        {
            this.year = year;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Padding = new MarginPadding
            {
                Vertical = 20,
                Left = 30,
                Right = 50
            };

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Children = new Drawable[]
                {
                    content = new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 10)
                    },
                    showMore = new ShowMoreButton
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Margin = new MarginPadding
                        {
                            Top = 15
                        },
                        Action = performFetch,
                        Alpha = 0
                    }
                }
            };

            performFetch();
        }

        private void performFetch()
        {
            request?.Cancel();

            request = new GetNewsRequest(year, lastCursor);
            request.Success += response => Schedule(() => onSuccess(response));
            api.PerformAsync(request);
        }

        private CancellationTokenSource cancellationToken;

        private void onSuccess(GetNewsResponse response)
        {
            cancellationToken?.Cancel();

            // only needs to be updated on the initial load, as the content won't change during pagination.
            if (lastCursor == null)
                SidebarMetadataUpdated?.Invoke(response.SidebarMetadata);

            // store cursor for next pagination request.
            lastCursor = response.Cursor;

            LoadComponentsAsync(response.NewsPosts.Select(p => new NewsCard(p)).ToList(), loaded =>
            {
                content.AddRange(loaded);

                showMore.IsLoading = false;
                showMore.Alpha = response.Cursor != null ? 1 : 0;
            }, (cancellationToken = new CancellationTokenSource()).Token);
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            cancellationToken?.Cancel();
            base.Dispose(isDisposing);
        }
    }
}
