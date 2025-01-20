namespace Trelnex.Core.Data;

public interface ICommandProviderFactory
{
    /// <summary>
    /// Gets the <see cref="CommandProviderStatus"/> of the command provider.
    /// </summary>
    /// <returns>The <see cref="CommandProviderStatus"/> of the command provider.</returns>
    CommandProviderFactoryStatus GetStatus();
}
