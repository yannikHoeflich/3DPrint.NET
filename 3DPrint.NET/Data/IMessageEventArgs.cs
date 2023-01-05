namespace _3DPrint.NET.Data;

internal interface IMessageEventArgs {
    SerialMessage Message { get; }
    bool FurtherProcessing { get; set; }
    bool DefaultProcessing { get; set; }

    void OverrideMessage(string message);

    void PreventFurtherProcessing() {
        FurtherProcessing = false;
        PreventDefaultProccessing();
    }

    void PreventDefaultProccessing() => DefaultProcessing = true;
}
