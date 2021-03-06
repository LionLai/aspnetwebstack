﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Mvc.Async.Test
{
    public class AsyncResultWrapperTest
    {
        [Fact]
        public void Begin_AsynchronousCompletion()
        {
            // Arrange
            AsyncCallback capturedCallback = null;
            IAsyncResult resultGivenToCallback = null;
            IAsyncResult innerResult = new MockAsyncResult();

            // Act
            IAsyncResult outerResult = AsyncResultWrapper.Begin(
                ar => { resultGivenToCallback = ar; },
                null,
                (callback, state) =>
                {
                    capturedCallback = callback;
                    return innerResult;
                },
                ar => { });

            capturedCallback(innerResult);

            // Assert
            Assert.Equal(outerResult, resultGivenToCallback);
        }

        [Fact]
        public void Begin_AsynchronousCompletionWithState()
        {
            // Arrange
            IAsyncResult innerResult = new MockAsyncResult();
            object invokeState = new object();
            object capturedBeginState = null;
            object capturedEndState = null;
            object expectedRetun = new object();

            // Act
            IAsyncResult outerResult = AsyncResultWrapper.Begin(
                null,
                null,
                (AsyncCallback callback, object callbackState, object innerInvokeState) =>
                {
                    capturedBeginState = innerInvokeState;
                    return innerResult;
                },
                (IAsyncResult result, object innerInvokeState) =>
                {
                    capturedEndState = innerInvokeState;
                    return expectedRetun;
                },
                invokeState,
                null,
                Timeout.Infinite);
            object endResult = AsyncResultWrapper.End<object>(outerResult);

            // Assert
            Assert.Same(expectedRetun, endResult);
            Assert.Same(invokeState, capturedBeginState);
            Assert.Same(invokeState, capturedEndState);
        }

        [Fact]
        public void Begin_ReturnsAsyncResultWhichWrapsInnerResult()
        {
            // Arrange
            IAsyncResult innerResult = new MockAsyncResult()
            {
                AsyncState = "inner state",
                CompletedSynchronously = true,
                IsCompleted = true
            };

            // Act
            IAsyncResult outerResult = AsyncResultWrapper.Begin(
                null, "outer state",
                (callback, state) => innerResult,
                ar => { });

            // Assert
            Assert.Equal(innerResult.AsyncState, outerResult.AsyncState);
            Assert.Null(outerResult.AsyncWaitHandle);
            Assert.Equal(innerResult.CompletedSynchronously, outerResult.CompletedSynchronously);
            Assert.Equal(innerResult.IsCompleted, outerResult.IsCompleted);
        }

        [Fact]
        public void Begin_SynchronousCompletion()
        {
            // Arrange
            IAsyncResult resultGivenToCallback = null;
            IAsyncResult innerResult = new MockAsyncResult();

            // Act
            IAsyncResult outerResult = AsyncResultWrapper.Begin(
                ar => { resultGivenToCallback = ar; },
                null,
                (callback, state) =>
                {
                    callback(innerResult);
                    return innerResult;
                },
                ar => { });

            // Assert
            Assert.Equal(outerResult, resultGivenToCallback);
        }

        [Fact]
        public void Begin_AsynchronousButAlreadyCompleted()
        {
            // Arrange
            Mock<IAsyncResult> innerResultMock = new Mock<IAsyncResult>();
            innerResultMock.Setup(ir => ir.CompletedSynchronously).Returns(false);
            innerResultMock.Setup(ir => ir.IsCompleted).Returns(true);

            // Act
            IAsyncResult outerResult = AsyncResultWrapper.Begin(
                null,
                null,
                (callback, state) =>
                {
                    callback(innerResultMock.Object);
                    return innerResultMock.Object;
                },
                ar => { });

            // Assert
            Assert.True(outerResult.CompletedSynchronously);
        }

        [Fact]
        public void BeginSynchronous_Action()
        {
            // Arrange
            bool actionCalled = false;

            // Act
            IAsyncResult asyncResult = AsyncResultWrapper.BeginSynchronous(callback: null, state: null, action: delegate { actionCalled = true; }, tag: null);
            AsyncResultWrapper.End(asyncResult);

            // Assert
            Assert.True(actionCalled);
            Assert.True(asyncResult.IsCompleted);
            Assert.True(asyncResult.CompletedSynchronously);
        }

        [Fact]
        public void BeginSynchronous_Func()
        {
            object expectedReturn = new object();
            object expectedState = new object();
            object expectedCallbackState = new object();
            bool funcCalled = false;
            bool asyncCalledbackCalled = false;

            // Act
            IAsyncResult asyncResult = AsyncResultWrapper.BeginSynchronous(
                callback: (innerIAsyncResult) =>
                {
                    asyncCalledbackCalled = true;
                    Assert.NotNull(innerIAsyncResult);
                    Assert.Same(expectedCallbackState, innerIAsyncResult.AsyncState);
                    Assert.True(innerIAsyncResult.IsCompleted);
                    Assert.True(innerIAsyncResult.CompletedSynchronously);
                },
                callbackState: expectedCallbackState,
                func: (innerIAsyncResult, innerState) =>
                {
                    funcCalled = true;
                    Assert.NotNull(innerIAsyncResult);
                    Assert.Same(expectedCallbackState, innerIAsyncResult.AsyncState);
                    Assert.Same(expectedState, innerState);
                    Assert.True(innerIAsyncResult.IsCompleted);
                    Assert.True(innerIAsyncResult.CompletedSynchronously);
                    return expectedReturn;
                },
                funcState: expectedState,
                tag: null);
            object retVal = AsyncResultWrapper.End<object>(asyncResult);

            // Assert
            Assert.Same(expectedReturn, retVal);
            Assert.True(asyncResult.IsCompleted);
            Assert.True(asyncResult.CompletedSynchronously);
            Assert.True(funcCalled);
            Assert.True(asyncCalledbackCalled);
        }

        [Fact]
        public void End_ExecutesStoredDelegateAndReturnsValue()
        {
            // Arrange
            IAsyncResult asyncResult = AsyncResultWrapper.Begin(
                null, null,
                (callback, state) => new MockAsyncResult(),
                ar => 42);

            // Act
            int returned = AsyncResultWrapper.End<int>(asyncResult);

            // Assert
            Assert.Equal(42, returned);
        }

        [Fact]
        public void End_ThrowsIfAsyncResultIsIncorrectType()
        {
            // Arrange
            IAsyncResult asyncResult = AsyncResultWrapper.Begin(
                null, null,
                (callback, state) => new MockAsyncResult(),
                ar => { });

            // Act & assert
            Assert.Throws<ArgumentException>(
                delegate { AsyncResultWrapper.End<int>(asyncResult); },
                "The provided IAsyncResult is not valid for this method." + Environment.NewLine
              + "Parameter name: asyncResult");
        }

        [Fact]
        public void End_ThrowsIfAsyncResultIsNull()
        {
            // Act & assert
            Assert.ThrowsArgumentNull(
                delegate { AsyncResultWrapper.End(null); }, "asyncResult");
        }

        [Fact]
        public void End_ThrowsIfAsyncResultTagMismatch()
        {
            // Arrange
            IAsyncResult asyncResult = AsyncResultWrapper.Begin(
                null, null,
                (callback, state) => new MockAsyncResult(),
                ar => { },
                "some tag");

            // Act & assert
            Assert.Throws<ArgumentException>(
                delegate { AsyncResultWrapper.End(asyncResult, "some other tag"); },
                "The provided IAsyncResult is not valid for this method." + Environment.NewLine
              + "Parameter name: asyncResult");
        }

        [Fact]
        public void End_ThrowsIfCalledTwiceOnSameAsyncResult()
        {
            // Arrange
            IAsyncResult asyncResult = AsyncResultWrapper.Begin(
                null, null,
                (callback, state) => new MockAsyncResult(),
                ar => { });

            // Act & assert
            AsyncResultWrapper.End(asyncResult);
            Assert.Throws<InvalidOperationException>(
                delegate { AsyncResultWrapper.End(asyncResult); },
                "The provided IAsyncResult has already been consumed.");
        }

        [Fact]
        public void TimedOut()
        {
            // Arrange
            ManualResetEvent waitHandle = new ManualResetEvent(false /* initialState */);

            AsyncCallback callback = ar => { waitHandle.Set(); };

            // Act & assert
            IAsyncResult asyncResult = AsyncResultWrapper.Begin(
                callback, null,
                (innerCallback, innerState) => new MockAsyncResult(),
                ar => { Assert.True(false, "This callback should never execute since we timed out."); },
                null, 0);

            // wait for the timeout
            waitHandle.WaitOne();

            Assert.True(asyncResult.IsCompleted);
            Assert.Throws<TimeoutException>(
                delegate { AsyncResultWrapper.End(asyncResult); });
        }
    }
}
