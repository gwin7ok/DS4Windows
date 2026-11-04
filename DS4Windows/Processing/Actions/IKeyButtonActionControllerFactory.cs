using System;

namespace DS4Windows.Processing.Actions
{
    public interface IKeyButtonActionControllerFactory
    {
        DS4Windows.Processing.Actions.KeyButtonActionController Create(int device, DS4Windows.Processing.Actions.KeyButtonActionController.Mode mode, string actionName = null);
        DS4Windows.Processing.Actions.KeyButtonActionController Create(int device, DS4Windows.Processing.SpecialAction sa, string actionName = null);
    }
}
