namespace Nox.Avatars.Parameters {
	[System.Flags]
	public enum ParameterFlags {
		None = 0,

		/// <summary>Le owner peut modifier la valeur sur son propre client.</summary>
		OwnerEditable        = 1 << 0,
		/// <summary>Les viewers peuvent modifier leur propre copie locale indépendamment.</summary>
		ViewerEditable       = 1 << 1,
		/// <summary>La valeur est sauvegardée et restaurée entre sessions.</summary>
		Persistent           = 1 << 2,
		/// <summary>Les viewers peuvent pousser des changements vers le client du owner.</summary>
		ViewerSyncsToOwner   = 1 << 3,
		/// <summary>Le owner pousse ses changements vers tous les viewers.</summary>
		OwnerSyncsToViewers  = 1 << 4,

		// --- Combinaisons courantes ---

		/// <summary>Sync bidirectionnel : les changements du owner et des viewers se propagent des deux côtés.</summary>
		BidirectionalSync    = ViewerSyncsToOwner  | OwnerSyncsToViewers,
		/// <summary>Le owner écrit localement et broadcast vers tous les viewers.</summary>
		OwnerBroadcast       = OwnerEditable       | OwnerSyncsToViewers,
		/// <summary>Tout le monde peut éditer et les changements se propagent dans les deux sens.</summary>
		FullSync             = OwnerEditable       | ViewerEditable | BidirectionalSync,
		/// <summary>Tous les participants peuvent éditer leur copie.</summary>
		AllEditable          = OwnerEditable       | ViewerEditable,
	}
}