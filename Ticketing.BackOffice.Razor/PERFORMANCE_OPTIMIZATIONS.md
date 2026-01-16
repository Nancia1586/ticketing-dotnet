# Optimisations de Performance - Ticketing.BackOffice.Razor

## 🔴 Causes Principales de Lenteur

### 1. **Ressources Externes (CDN)**
- **Tailwind CSS** : Chargé depuis CDN à chaque page
- **Flowbite** : CSS + JS depuis CDN
- **Google Fonts** : Polices Inter chargées depuis Google
- **Impact** : Latence réseau, dépendance externe

### 2. **Authentification/Authorization**
- Vérification des rôles sur chaque page (`User.IsInRole`)
- `UserManager.GetUserAsync()` appelé plusieurs fois
- Middleware d'authentification sur chaque requête
- **Impact** : Requêtes DB supplémentaires

### 3. **Requêtes de Base de Données**
- **Page Index** : Charge tous les événements, réservations, catégories
- **Page Details** : Charge tous les sièges et réservations (produit cartésien)
- Pas de pagination
- Pas de cache
- **Impact** : Requêtes SQL lourdes

### 4. **Scripts JavaScript**
- jQuery chargé de manière synchrone
- Scripts de validation bloquants
- Scripts dans le layout exécutés sur chaque page
- **Impact** : Blocage du rendu

### 5. **Middleware Pipeline**
- `UseRequestLocalization` sur chaque requête
- `UseAuthentication` + `UseAuthorization` sur chaque requête
- **Impact** : Overhead sur chaque requête

## ✅ Optimisations Recommandées

### 1. **Optimiser les Ressources Externes**
```html
<!-- Utiliser des ressources locales ou CDN avec preconnect -->
<link rel="preconnect" href="https://cdn.tailwindcss.com">
<link rel="dns-prefetch" href="https://fonts.googleapis.com">

<!-- Charger les polices de manière asynchrone -->
<link href="..." rel="stylesheet" media="print" onload="this.media='all'">

<!-- Scripts avec defer -->
<script src="..." defer></script>
```

### 2. **Optimiser l'Authentification**
- Utiliser `User.Claims` au lieu de `UserManager.GetUserAsync()`
- Mettre en cache les vérifications de rôles
- Éviter les appels DB répétés

### 3. **Optimiser les Requêtes DB**
- Utiliser `AsSplitQuery()` pour éviter les produits cartésiens
- Ajouter de la pagination
- Utiliser des projections (Select) au lieu de charger toutes les données
- Mettre en cache les données fréquemment accédées

### 4. **Optimiser les Scripts**
- Déplacer les scripts en fin de page
- Utiliser `defer` ou `async`
- Minifier et bundler les scripts

### 5. **Optimiser le Middleware**
- Réduire le nombre de middlewares
- Utiliser Response Caching pour les pages statiques

## 📊 Priorités d'Optimisation

1. **Haute Priorité** : Requêtes DB (AsSplitQuery, pagination)
2. **Moyenne Priorité** : Authentification (éviter GetUserAsync répétés)
3. **Basse Priorité** : Ressources externes (CDN, scripts)

