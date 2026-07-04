using System;
using Unity.AppUI.Editor;
using Unity.AppUI.UI;

namespace MyGame.Editor.Storybook
{
    public class ActionButtonStoryBookPage : StoryBookPage
    {
        public override string displayName => "Action Button";

        public override Type componentType => typeof(ActionButtonStoryBookComponent);

        public ActionButtonStoryBookPage()
        {
            m_Stories.Add(new StoryBookStory("Default", () => new ActionButton
            {
                label = "Action Button"
            }));

            m_Stories.Add(new StoryBookStory("With Icon", () => new ActionButton
            {
                label = "Action Button",
                icon = "info"
            }));

            m_Stories.Add(new StoryBookStory("Icon Only", () => new ActionButton
            {
                icon = "info"
            }));

            m_Stories.Add(new StoryBookStory("Selected", () => new ActionButton
            {
                label = "Selected Action Button",
                selected = true
            }));

            m_Stories.Add(new StoryBookStory("Quiet", () => new ActionButton
            {
                label = "Quiet Action Button",
                quiet = true
            }));

            m_Stories.Add(new StoryBookStory("Accent", () => new ActionButton
            {
                label = "Accent Action Button",
                accent = true
            }));
        }
    }

    public class ActionButtonStoryBookComponent : StoryBookComponent
    {
        public override Type uiElementType => typeof(ActionButton);

        public ActionButtonStoryBookComponent()
        {
            m_Properties.Add(new StoryBookStringProperty(
                nameof(ActionButton.label),
                (el) => ((ActionButton)el).label,
                (el, val) => ((ActionButton)el).label = val));

            m_Properties.Add(new StoryBookStringProperty(
                nameof(ActionButton.icon),
                (el) => ((ActionButton)el).icon,
                (el, val) => ((ActionButton)el).icon = val));

            m_Properties.Add(new StoryBookStringProperty(
                nameof(ActionButton.trailingIcon),
                (el) => ((ActionButton)el).trailingIcon,
                (el, val) => ((ActionButton)el).trailingIcon = val));

            m_Properties.Add(new StoryBookBooleanProperty(
                nameof(ActionButton.selected),
                (el) => ((ActionButton)el).selected,
                (el, val) => ((ActionButton)el).selected = val));

            m_Properties.Add(new StoryBookBooleanProperty(
                nameof(ActionButton.quiet),
                (el) => ((ActionButton)el).quiet,
                (el, val) => ((ActionButton)el).quiet = val));

            m_Properties.Add(new StoryBookBooleanProperty(
                nameof(ActionButton.accent),
                (el) => ((ActionButton)el).accent,
                (el, val) => ((ActionButton)el).accent = val));

            m_Properties.Add(new StoryBookEnumProperty<Size>(
                nameof(ActionButton.size),
                (el) => ((ActionButton)el).size,
                (el, val) => ((ActionButton)el).size = val));
        }
    }
}
