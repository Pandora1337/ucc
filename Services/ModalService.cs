using Microsoft.JSInterop;

namespace ucc.Services;

public sealed class ModalService(IJSRuntime jSRuntime)
{
    private IJSRuntime JS = jSRuntime;
    private int counter = 0;

    public async void AddModal()
    {
        counter++;
        // await JS.InvokeVoidAsync("setInert", modal.ElementReference, false);;
        await JS.InvokeVoidAsync("eval", $"document.body.classList.add('overflow-hidden');");
    }

    public async void RemoveModal()
    {
        counter--;
        if (counter > 0)
            return;

        await JS.InvokeVoidAsync("eval", $"document.body.classList.remove('overflow-hidden');");
    }
}