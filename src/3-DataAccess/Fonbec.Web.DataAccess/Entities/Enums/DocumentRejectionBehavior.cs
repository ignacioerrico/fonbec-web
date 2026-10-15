namespace Fonbec.Web.DataAccess.Entities.Enums;

/// <summary>
/// What a filial does after a reviewer rejects a document.
/// </summary>
public enum DocumentRejectionBehavior : byte
{
    /// <summary>
    /// The facilitator sees the rejection immediately and receives an email.
    /// </summary>
    ReturnToFacilitator = 0,

    /// <summary>
    /// The document waits until a manager shares it or sends it back.
    /// </summary>
    HoldForManager = 1,
}