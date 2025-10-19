// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// this file mainly copied from osu.Framework.Input.Handlers.Mouse

#nullable disable

using System.Text.Json;
using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.student
{
    public class ApiInputHandler : InputHandler
    {
        private static readonly GlobalStatistic<ulong> statistic_total_events = GlobalStatistics.Get<ulong>(StatisticGroupFor<ApiInputHandler>(), "Total events");

        /// <summary>
        /// Whether relative mode should be preferred when the window has focus, the cursor is contained and the OS cursor is not visible.
        /// </summary>
        public BindableBool UseRelativeMode { get; } = new BindableBool(true)
        {
            Description = "Allows for sensitivity adjustment and tighter control of input",
        };

        public BindableDouble Sensitivity { get; } = new BindableDouble(1)
        {
            MinValue = 0.1,
            MaxValue = 10,
            Precision = 0.01
        };

        public ApiInputHandler()
        {

        }


        public override string Description => "APIInput";

        public override bool IsActive => true;

        private Vector2? lastPosition;

        public bool PerformAction(JsonElement actionObject)
        {
            bool isActionHandled = false;

            // 2. 檢查 'mouseButtonDown' 屬性
            if (actionObject.TryGetProperty("MouseLeftBtn", out var MouseLeftBtn) && MouseLeftBtn.ValueKind == JsonValueKind.Number)
            {
                var mb = MouseButton.Left;
                if (MouseLeftBtn.GetInt32() == 0)
                {
                    handleMouseDown(mb);
                    isActionHandled = true;
                }
                else if (MouseLeftBtn.GetInt32() == 1)
                {
                    handleMouseUp(mb);
                    isActionHandled = true;
                }
            }
            // 檢查 'move' 物件
            if (actionObject.TryGetProperty("move", out var moveElement))
            {
                if (moveElement.ValueKind == JsonValueKind.Array && moveElement.GetArrayLength() == 2)
                {
                    var xElement = moveElement[0]; // 在 .NET 6+ 中，JsonElement 支援陣列索引器
                    var yElement = moveElement[1];

                    if (xElement.TryGetSingle(out float x) && yElement.TryGetSingle(out float y))
                    {
                        Vector2 pos = new(x, y);
                        HandleMouseMove(pos);
                        isActionHandled = true;
                    }
                    else
                    {
                        Logger.Log($"'move' 陣列中的元素必須是數字。收到的資料: {moveElement.GetRawText()}", LoggingTarget.Input);
                    }
                }
                else
                {
                    Logger.Log($"'move' 屬性必須是一個包含兩個元素的陣列。收到的資料: {moveElement.GetRawText()}", LoggingTarget.Input);
                }
            }

            return isActionHandled;
        }


        public override void Reset()
        {
            Sensitivity.SetDefault();
            base.Reset();
        }


        protected virtual void HandleMouseMove(Vector2 position)
        {
            enqueueInput(new MousePositionAbsoluteInput { Position = position });
        }

        protected virtual void HandleMouseMoveRelative(Vector2 delta)
        {
            enqueueInput(new MousePositionRelativeInput { Delta = delta * (float)Sensitivity.Value });
        }

        private void handleMouseDown(MouseButton button) => enqueueInput(new MouseButtonInput(button, true));

        private void handleMouseUp(MouseButton button) => enqueueInput(new MouseButtonInput(button, false));

        private void handleMouseWheel(Vector2 delta, bool precise) => enqueueInput(new MouseScrollRelativeInput { Delta = delta, IsPrecise = precise });

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            statistic_total_events.Value++;
        }

    }
}

