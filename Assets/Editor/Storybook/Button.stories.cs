using System;
using Unity.AppUI.Editor;
using Unity.AppUI.UI;
using Button = Unity.AppUI.UI.Button;

namespace MyGame.Editor.Storybook
{
    public class ButtonStoryBookPage : StoryBookPage
    {
        public override string displayName => "Button";

        public override Type componentType => typeof(ButtonStoryBookComponent);

        public ButtonStoryBookPage()
        {
            m_Stories.Add(new StoryBookStory("Default", () => new Button
            {
                title = "Default Button"
            }));

            m_Stories.Add(new StoryBookStory("Accent", () => new Button
            {
                variant = ButtonVariant.Accent,
                title = "Accent Button"
            }));

            m_Stories.Add(new StoryBookStory("Destructive", () => new Button
            {
                variant = ButtonVariant.Destructive,
                title = "Destructive Button"
            }));

            m_Stories.Add(new StoryBookStory("Quiet", () => new Button
            {
                quiet = true,
                title = "Quiet Button"
            }));

            m_Stories.Add(new StoryBookStory("With Subtitle", () => new Button
            {
                title = "Button Title",
                subtitle = "Button Subtitle"
            }));

            m_Stories.Add(new StoryBookStory("With Icons", () => new Button
            {
                title = "Button With Icons",
                leadingIcon = "info",
                trailingIcon = "caret-right"
            }));
        }
    }

    public class ButtonStoryBookComponent : StoryBookComponent
    {
        public override Type uiElementType => typeof(Button);

        public ButtonStoryBookComponent()
        {
            m_Properties.Add(new StoryBookEnumProperty<ButtonVariant>(
                nameof(Button.variant),
                (el) => ((Button)el).variant,
                (el, val) => ((Button)el).variant = val));

            m_Properties.Add(new StoryBookBooleanProperty(
                nameof(Button.quiet),
                (el) => ((Button)el).quiet,
                (el, val) => ((Button)el).quiet = val));

            m_Properties.Add(new StoryBookStringProperty(
                nameof(Button.title),
                (el) => ((Button)el).title,
                (el, val) => ((Button)el).title = val));

            m_Properties.Add(new StoryBookStringProperty(
                nameof(Button.subtitle),
                (el) => ((Button)el).subtitle,
                (el, val) => ((Button)el).subtitle = val));

            m_Properties.Add(new StoryBookStringProperty(
                nameof(Button.leadingIcon),
                (el) => ((Button)el).leadingIcon,
                (el, val) => ((Button)el).leadingIcon = val));

            m_Properties.Add(new StoryBookStringProperty(
                nameof(Button.trailingIcon),
                (el) => ((Button)el).trailingIcon,
                (el, val) => ((Button)el).trailingIcon = val));

            m_Properties.Add(new StoryBookEnumProperty<Size>(
                nameof(Button.size),
                (el) => ((Button)el).size,
                (el, val) => ((Button)el).size = val));
        }
    }
}
