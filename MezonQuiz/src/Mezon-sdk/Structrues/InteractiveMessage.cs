using Mezon_sdk.Models;

namespace Mezon_sdk.Structures
{
    public class InteractiveBuilder
    {
        private readonly Dictionary<string, object> _interactive;
        private static readonly Random _random = new Random();

        public InteractiveBuilder(string? title = null)
        {
            _interactive = new Dictionary<string, object>
            {
                { "color", GetRandomColor() },
                { "fields", new List<Dictionary<string, object>>() },
                { "timestamp", DateTime.UtcNow.ToString("O") },
                { "footer", new Dictionary<string, object>
                    {
                        { "text", "Powered by Mezon" },
                        { "icon_url", "https://cdn.mezon.vn/1837043892743049216/1840654271217930240/1827994776956309500/857_0246x0w.webp" }
                    }
                }
            };

            if (title != null)
            {
                _interactive["title"] = title;
            }
        }

        private static string GetRandomColor()
        {
            return $"#{_random.Next(0, 0xFFFFFF):X6}";
        }

        public InteractiveBuilder SetColor(string color)
        {
            _interactive["color"] = color;
            return this;
        }

        public InteractiveBuilder SetTitle(string title)
        {
            _interactive["title"] = title;
            return this;
        }

        public InteractiveBuilder SetUrl(string url)
        {
            _interactive["url"] = url;
            return this;
        }

        public InteractiveBuilder SetAuthor(string name, string? iconUrl = null, string? url = null)
        {
            var author = new Dictionary<string, object> { { "name", name } };
            if (!string.IsNullOrEmpty(iconUrl))
            {
                author["icon_url"] = iconUrl;
            }
            if (!string.IsNullOrEmpty(url))
            {
                author["url"] = url;
            }
            _interactive["author"] = author;
            return this;
        }

        public InteractiveBuilder SetDescription(string description)
        {
            _interactive["description"] = description;
            return this;
        }

        public InteractiveBuilder SetThumbnail(string url)
        {
            _interactive["thumbnail"] = new Dictionary<string, object> { { "url", url } };
            return this;
        }

        public InteractiveBuilder SetImage(string url, string? width = null, string? height = null)
        {
            _interactive["image"] = new Dictionary<string, object>
            {
                { "url", url },
                { "width", width ?? "auto" },
                { "height", height ?? "auto" }
            };
            return this;
        }

        public InteractiveBuilder SetFooter(string text, string? iconUrl = null)
        {
            var footer = new Dictionary<string, object> { { "text", text } };
            if (!string.IsNullOrEmpty(iconUrl))
            {
                footer["icon_url"] = iconUrl;
            }
            _interactive["footer"] = footer;
            return this;
        }

        public InteractiveBuilder AddField(string name, string value, bool inline = false)
        {
            var fields = (List<Dictionary<string, object>>)_interactive["fields"];
            fields.Add(new Dictionary<string, object>
            {
                { "name", name },
                { "value", value },
                { "inline", inline }
            });
            return this;
        }

        public InteractiveBuilder AddInputField(
            string fieldId,
            string name,
            string? placeholder = null,
            InputFieldOption? options = null,
            string? description = null)
        {
            var fields = (List<Dictionary<string, object>>)_interactive["fields"];

            var component = new Dictionary<string, object>
            {
                { "id", $"{fieldId}-component" },
                { "defaultValue", options != null && options.Defaultvalue != null ? options.Defaultvalue : "" },
                { "type", options?.Type ?? "text" },
                { "textarea", options?.Textarea ?? false }
            };

            if (placeholder != null)
            {
                component["placeholder"] = placeholder;
            }

            var inputs = new Dictionary<string, object>
            {
                { "id", fieldId },
                { "type", (int)MessageComponentType.Input },
                { "component", component }
            };

            fields.Add(new Dictionary<string, object>
            {
                { "name", name },
                { "value", description ?? "" },
                { "inputs", inputs }
            });
            return this;
        }

        public InteractiveBuilder AddSelectField(
            string fieldId,
            string name,
            List<SelectFieldOption> options,
            SelectFieldOption? valueSelected = null,
            string? description = null)
        {
            var fields = (List<Dictionary<string, object>>)_interactive["fields"];

            var component = new Dictionary<string, object>
            {
                { "options", options }
            };

            if (valueSelected != null)
            {
                component["valueSelected"] = valueSelected;
            }

            var inputs = new Dictionary<string, object>
            {
                { "id", fieldId },
                { "type", (int)MessageComponentType.Select },
                { "component", component }
            };

            fields.Add(new Dictionary<string, object>
            {
                { "name", name },
                { "value", description ?? "" },
                { "inputs", inputs }
            });
            return this;
        }

        public InteractiveBuilder AddRadioField(
            string fieldId,
            string name,
            List<RadioFieldOption> options,
            string? description = null,
            int? maxOptions = null)
        {
            var fields = (List<Dictionary<string, object>>)_interactive["fields"];
            var inputs = new Dictionary<string, object>
            {
                { "id", fieldId },
                { "type", (int)MessageComponentType.Radio },
                { "component", options }
            };

            if (maxOptions.HasValue)
            {
                inputs["max_options"] = maxOptions.Value;
            }

            fields.Add(new Dictionary<string, object>
            {
                { "name", name },
                { "value", description ?? "" },
                { "inputs", inputs }
            });
            return this;
        }

        public InteractiveBuilder AddDatepickerField(
            string fieldId,
            string name,
            string? description = null)
        {
            var fields = (List<Dictionary<string, object>>)_interactive["fields"];
            var inputs = new Dictionary<string, object>
            {
                { "id", fieldId },
                { "type", (int)MessageComponentType.Datepicker },
                { "component", new Dictionary<string, object>() }
            };

            fields.Add(new Dictionary<string, object>
            {
                { "name", name },
                { "value", description ?? "" },
                { "inputs", inputs }
            });
            return this;
        }

        public InteractiveBuilder AddAnimation(
            string fieldId,
            AnimationConfig config,
            string? name = null,
            string? description = null)
        {
            var fields = (List<Dictionary<string, object>>)_interactive["fields"];
            var inputs = new Dictionary<string, object>
            {
                { "id", fieldId },
                { "type", (int)MessageComponentType.Animation },
                { "component", config }
            };

            fields.Add(new Dictionary<string, object>
            {
                { "name", name ?? "" },
                { "value", description ?? "" },
                { "inputs", inputs }
            });
            return this;
        }

        public Dictionary<string, object> Build()
        {
            return _interactive;
        }
    }
}