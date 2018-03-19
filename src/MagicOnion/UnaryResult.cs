using Grpc.Core;
using MagicOnion.CompilerServices;
using MessagePack;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.ConfiguredTaskAwaitable;

namespace MagicOnion
{
    /// <summary>
    /// Wrapped AsyncUnaryCall.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncUnaryResultMethodBuilder<>))]
    public struct UnaryResult<TResponse>
    {
        internal readonly bool hasRawValue; // internal
		internal readonly ValueTask<TResponse> rawValueTask; // internal

        readonly AsyncUnaryCall<byte[]> inner;
        readonly IFormatterResolver resolver;

        public UnaryResult(TResponse rawValue)
        {
            this.hasRawValue = true;
			this.rawValueTask = new ValueTask<TResponse>(rawValue);
            this.inner = null;
            this.resolver = null;
        }

        public UnaryResult(Task<TResponse> rawTaskValue)
        {
            this.hasRawValue = true;
			this.rawValueTask = new ValueTask<TResponse>(rawTaskValue);
            this.inner = null;
            this.resolver = null;
        }

        public UnaryResult(AsyncUnaryCall<byte[]> inner, IFormatterResolver resolver)
        {
            this.hasRawValue = false;
			this.rawValueTask = new ValueTask<TResponse>();
            this.inner = inner;
            this.resolver = resolver;
        }

        async Task<TResponse> Deserialize()
        {
            var bytes = await inner.ResponseAsync.ConfigureAwait(false);
            return LZ4MessagePackSerializer.Deserialize<TResponse>(bytes, resolver);
        }

        /// <summary>
        /// Asynchronous call result.
        /// </summary>
        public ValueTask<TResponse> ResponseAsync
        {
            get
            {
                if (!hasRawValue)
                {
					return new ValueTask<TResponse>(Deserialize());
                }
				else
					return this.rawValueTask;
            }
        }

        /// <summary>
        /// Asynchronous access to response headers.
        /// </summary>
        public Task<Metadata> ResponseHeadersAsync
        {
            get
            {
                return inner.ResponseHeadersAsync;
            }
        }

        /// <summary>
        /// Allows awaiting this object directly.
        /// </summary>
        public ValueTaskAwaiter<TResponse> GetAwaiter()
        {
            return ResponseAsync.GetAwaiter();
        }

		/// <summary>Configures an awaiter used to await this task>.</summary>
		/// <param name="continueOnCapturedContext">
		/// true to attempt to marshal the continuation back to the original context captured; otherwise, false.
		/// </param>
		/// <returns>An object used to await this task.</returns>
		public IConfiguredTaskAwaitable<TResponse> ConfigureAwait(bool continueOnCapturedContext) =>
		  new ConfiguredValueTaskAwaitableWrapper<TResponse>(ResponseAsync.ConfigureAwait(continueOnCapturedContext));

        /// <summary>
        /// Gets the call status if the call has already finished.
        /// Throws InvalidOperationException otherwise.
        /// </summary>
        public Status GetStatus()
        {
            return inner.GetStatus();
        }

        /// <summary>
        /// Gets the call trailing metadata if the call has already finished.
        /// Throws InvalidOperationException otherwise.
        /// </summary>
        public Metadata GetTrailers()
        {
            return inner.GetTrailers();
        }

        /// <summary>
        /// Provides means to cleanup after the call.
        /// If the call has already finished normally (request stream has been completed and call result has been received), doesn't do anything.
        /// Otherwise, requests cancellation of the call which should terminate all pending async operations associated with the call.
        /// As a result, all resources being used by the call should be released eventually.
        /// </summary>
        /// <remarks>
        /// Normally, there is no need for you to dispose the call unless you want to utilize the
        /// "Cancel" semantics of invoking <c>Dispose</c>.
        /// </remarks>
        public void Dispose()
        {
            inner.Dispose();
        }

		private class ConfiguredValueTaskAwaitableWrapper<TResult> : IConfiguredTaskAwaitable<TResult>
		{
			private readonly ConfiguredValueTaskAwaitable<TResult> _cfgAwaitable;

			public ConfiguredValueTaskAwaitableWrapper(ConfiguredValueTaskAwaitable<TResult> cfgAwaitable)
			{
				_cfgAwaitable = cfgAwaitable;
			}

			ITaskAwaiter<TResult> IConfiguredTaskAwaitable<TResult>.GetAwaiter() => new ConfiguredValueTaskAwaiterWrapper(_cfgAwaitable.GetAwaiter());

			public class ConfiguredValueTaskAwaiterWrapper : ITaskAwaiter<TResult>
			{
				private readonly ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter _cfgAwaiter;

				public ConfiguredValueTaskAwaiterWrapper(ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter cfgAwaiter)
				{
					_cfgAwaiter = cfgAwaiter;
				}

				bool ITaskAwaiter<TResult>.IsCompleted => _cfgAwaiter.IsCompleted;

				void INotifyCompletion.OnCompleted(Action continuation) => _cfgAwaiter.OnCompleted(continuation);

				void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => _cfgAwaiter.UnsafeOnCompleted(continuation);

				TResult ITaskAwaiter<TResult>.GetResult() => _cfgAwaiter.GetResult();
			}
		}
	}

	public interface ITaskAwaiter<out TResult> : ICriticalNotifyCompletion
	{
		/// <summary>Gets whether the task being awaited is completed.</summary>
		/// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
		/// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
		bool IsCompleted { get; }

		/// <summary>Ends the await on the completed <see cref="System.Threading.Tasks.Task"/>.</summary>
		/// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
		/// <exception cref="System.Threading.Tasks.TaskCanceledException">The task was canceled.</exception>
		/// <exception cref="System.Exception">The task completed in a Faulted state.</exception>
		TResult GetResult();
	}

	public interface IConfiguredTaskAwaitable<out TResult>
	{
		/// <summary>Gets an awaiter for this awaitable.</summary>
		/// <returns>The awaiter.</returns>
		ITaskAwaiter<TResult> GetAwaiter();
	}
}