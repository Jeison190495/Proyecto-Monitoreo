namespace CyberGuardArch.Core.Interfaces;

public interface ICommandHistoryService
{
    List<string> GetLastCommands(string filePath, int count = 3);
}
