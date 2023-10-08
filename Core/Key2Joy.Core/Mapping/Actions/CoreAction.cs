﻿using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Plugins;

namespace Key2Joy.Mapping.Actions
{
    public abstract class CoreAction : AbstractAction
    {
        public string ImageResource { get; set; }

        public CoreAction(string name)
            : base(name)
        {
        }

        public static AbstractAction MakeAction(MappingTypeFactory<AbstractAction> actionFactory)
        {
            var typeAttribute = actionFactory.Attribute;
            var action = actionFactory.CreateInstance(new object[]
            {
                typeAttribute.NameFormat ?? actionFactory.FullTypeName,
            });

            return action;
        }

        public AbstractAction MakeStartedAction(MappingTypeFactory<AbstractAction> actionFactory)
        {
            var typeAttribute = actionFactory.Attribute;
            var action = actionFactory.CreateInstance(new object[]
            {
                typeAttribute.NameFormat ?? actionFactory.FullTypeName,
            });
            action.SetStartData(this.listener, ref this.otherActions);
            return action;
        }

        public override string ToString()
        {
            return this.GetNameDisplay();
        }
    }
}
