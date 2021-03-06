﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Web.Mvc
{
    public class ModelBinderProviderCollection : Collection<IModelBinderProvider>
    {
        private IModelBinderProvider[] _combinedItems;
        private IDependencyResolver _dependencyResolver;

        public ModelBinderProviderCollection()
        {
        }

        public ModelBinderProviderCollection(IList<IModelBinderProvider> list)
            : base(list)
        {
        }

        internal ModelBinderProviderCollection(IList<IModelBinderProvider> list, IDependencyResolver dependencyResolver)
            : base(list)
        {
            _dependencyResolver = dependencyResolver;
        }

        internal IModelBinderProvider[] CombinedItems
        {
            get
            {
                IModelBinderProvider[] combinedItems = _combinedItems;
                if (combinedItems == null)
                {
                    combinedItems = MultiServiceResolver.GetCombined<IModelBinderProvider>(Items, _dependencyResolver);
                    _combinedItems = combinedItems;
                }
                return combinedItems;
            }
        }

        protected override void InsertItem(int index, IModelBinderProvider item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            _combinedItems = null;
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            _combinedItems = null;
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, IModelBinderProvider item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            _combinedItems = null;
            base.SetItem(index, item);
        }

        public IModelBinder GetBinder(Type modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException("modelType");
            }

            var modelBinders = from providers in CombinedItems
                               let modelBinder = providers.GetBinder(modelType)
                               where modelBinder != null
                               select modelBinder;

            return modelBinders.FirstOrDefault();
        }
    }
}
