namespace Swn.Workflow.Application;

public interface ISecretProtector
{
    string Protect(string plainText);

    string Unprotect(string protectedText);
}