﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;


namespace MudBlazor.Services
{
    /// <summary>
    /// This service listens to browser resize events and allows you to react to a changing window size in Blazor
    /// </summary>
    public class ResizeListenerService : IResizeListenerService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsRuntime"></param>
        /// <param name="options"></param>
        public ResizeListenerService(IJSRuntime jsRuntime, IOptions<ResizeOptions> options = null)
        {
            this._options = options.Value ?? new ResizeOptions();
            this._jsRuntime = jsRuntime;
        }

        private readonly IJSRuntime _jsRuntime;
        private readonly ResizeOptions _options;
#nullable enable
        private EventHandler<BrowserWindowSize>? _onResized;

        /// <summary>
        /// Subscribe to the browsers resize() event.
        /// </summary>
        public event EventHandler<BrowserWindowSize>? OnResized
        {
            add => Subscribe(value);
            remove => Unsubscribe(value);
        }
#nullable disable

        private void Unsubscribe(EventHandler<BrowserWindowSize> value)
        {
            _onResized -= value;
            if (_onResized == null)
            {
                Cancel().ConfigureAwait(false);
            }
        }

        private void Subscribe(EventHandler<BrowserWindowSize> value)
        {
            if (_onResized == null)
            {
                Task.Run(async () => await Start());
            }
            _onResized += value;
        }

        private async ValueTask<bool> Start() =>
            await _jsRuntime.InvokeAsync<bool>($"resizeListener.listenForResize", DotNetObjectReference.Create(this), _options);

        private async ValueTask Cancel()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync($"resizeListener.cancelListener");
            }
            catch (Exception)
            {
                /* ignore */
            }
        }

        /// <summary>
        /// Determine if the Document matches the provided media query.
        /// </summary>
        /// <param name="mediaQuery"></param>
        /// <returns>Returns true if matched.</returns>
        public async ValueTask<bool> MatchMedia(string mediaQuery) =>
            await _jsRuntime.InvokeAsync<bool>($"resizeListener.matchMedia", mediaQuery);

        /// <summary>
        /// Get the current BrowserWindowSize, this includes the Height and Width of the document.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<BrowserWindowSize> GetBrowserWindowSize() =>
            await _jsRuntime.InvokeAsync<BrowserWindowSize>($"resizeListener.getBrowserWindowSize");

        /// <summary>
        /// Invoked by jsInterop, use the OnResized event handler to subscribe.
        /// </summary>
        /// <param name="browserWindowSize"></param>
        [JSInvokable]
        public void RaiseOnResized(BrowserWindowSize browserWindowSize)
        {
            _windowSize = browserWindowSize;
            _onResized?.Invoke(this, browserWindowSize);
        }

        private BrowserWindowSize _windowSize;

        public Dictionary<Breakpoint, int> BreakpointDefinition { get; set; } = new Dictionary<Breakpoint, int>()
        {
            [Breakpoint.Xl] = 1920,
            [Breakpoint.Lg] = 1280,
            [Breakpoint.Md] = 960,
            [Breakpoint.Sm] = 600,
            [Breakpoint.Xs] = 0,
        };

        public async Task<Breakpoint> GetBreakpoint( ) {
            // note: we don't need to get the size if we are listening for updates, so only if onResized==null, get the actual size
            if (_onResized == null || _windowSize == null)
                _windowSize = await GetBrowserWindowSize();
            if (_windowSize == null)
                return Breakpoint.Xs;
            if (_windowSize.Width >= BreakpointDefinition[Breakpoint.Xl])
                return Breakpoint.Xl;
            else if (_windowSize.Width >= BreakpointDefinition[Breakpoint.Lg])
                return Breakpoint.Lg;
            else if (_windowSize.Width >= BreakpointDefinition[Breakpoint.Md])
                return Breakpoint.Md;
            else if (_windowSize.Width >= BreakpointDefinition[Breakpoint.Sm])
                return Breakpoint.Sm;
            else
                return Breakpoint.Xs;
        }

        public async Task<bool> IsMediaSize(Breakpoint breakpoint)
        {
            // note: we don't need to get the size if we are listening for updates, so only if onResized==null, get the actual size
            if (_onResized == null || _windowSize == null)
                _windowSize = await GetBrowserWindowSize();
            if (_windowSize == null)
                return false;
            if (_windowSize.Width >= BreakpointDefinition[Breakpoint.Xl])
            {
                if (breakpoint == Breakpoint.Xl || 
                    breakpoint == Breakpoint.LgAndUp || breakpoint == Breakpoint.MdAndUp || breakpoint == Breakpoint.SmAndUp)
                    return true;
            }
            else if (_windowSize.Width >= BreakpointDefinition[Breakpoint.Lg])
            {
                if (breakpoint == Breakpoint.Lg || 
                    breakpoint == Breakpoint.LgAndUp || breakpoint == Breakpoint.MdAndUp || breakpoint == Breakpoint.SmAndUp ||
                    breakpoint == Breakpoint.LgAndDown)
                    return true;
            }
            else if (_windowSize.Width >= BreakpointDefinition[Breakpoint.Md])
            {
                if (breakpoint == Breakpoint.Md ||
                    breakpoint == Breakpoint.MdAndUp || breakpoint == Breakpoint.SmAndUp ||
                    breakpoint == Breakpoint.MdAndDown)
                    return true;
            }
            else if (_windowSize.Width >= BreakpointDefinition[Breakpoint.Sm])
            {
                if (breakpoint == Breakpoint.Sm ||
                    breakpoint == Breakpoint.SmAndUp || 
                    breakpoint == Breakpoint.SmAndDown)
                    return true;
            }
            else if (breakpoint == Breakpoint.Xs)
                return true;
            return false;
        }

        bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Cancel();
                if (disposing)
                {
                    _onResized = null;
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }


    public interface IResizeListenerService : IDisposable
    {
#nullable enable
        event EventHandler<BrowserWindowSize>? OnResized;
#nullable disable
        ValueTask<BrowserWindowSize> GetBrowserWindowSize();
        Task<bool> IsMediaSize(Breakpoint breakpoint);
        Task<Breakpoint> GetBreakpoint();
    }
}
