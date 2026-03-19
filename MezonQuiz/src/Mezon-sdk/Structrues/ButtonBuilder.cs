using Mezon_sdk.Models;

namespace Mezon_sdk.Structures
{

    public class ButtonBuilder
    {
        public List<Dictionary<string, object>> Components { get; private set; }

        public ButtonBuilder()
        {
            Components = new List<Dictionary<string, object>>();
        }

        public ButtonBuilder AddButton(
            string componentId,
            string label,
            ButtonMessageStyle style,
            string? url = null,
            bool disabled = false)
        {
            var componentDict = new Dictionary<string, object>
            {
                { "label", label },
                { "style", (int)style }
            };

            if (!string.IsNullOrEmpty(url))
            {
                componentDict["url"] = url;
            }

            if (disabled)
            {
                componentDict["disable"] = true;
            }

            var buttonComponent = new Dictionary<string, object>
            {
                { "id", componentId },
                { "type", (int)MessageComponentType.Button },
                { "component", componentDict }
            };

            Components.Add(buttonComponent);
            return this;
        }

        public List<Dictionary<string, object>> Build()
        {
            return Components;
        }

        public ButtonBuilder Clear()
        {
            Components.Clear();
            return this;
        }
    }
}