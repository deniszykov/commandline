﻿// ReSharper disable once CheckNamespace
namespace System
{
    /// <summary>
    /// Provides data for the event that is raised when there is an exception that is  not handled in any application domain.
    /// </summary>
#if !NETSTANDARD13
    [Serializable]
#endif
    public sealed class ExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unhandled exception object.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ExceptionEventArgs class with
        /// the exception instance.
        /// </summary>
        /// <param name="exception">The exception that is not handled.</param>
        public ExceptionEventArgs(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            this.Exception = exception;
        }
    }
}