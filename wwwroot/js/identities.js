/**
 * Limbus Randomized Team Picker - Identity Catalog JavaScript
 * Handles loading, searching, sorting, and rendering of Identity cards.
 * Client-side selection state with IsSelected toggle.
 */

(function () {
    'use strict';

    // ============================================================
    // State
    // ============================================================
    let allIdentities = [];
    let filteredIdentities = [];

    // ============================================================
    // DOM References
    // ============================================================
    const searchInput = document.getElementById('searchInput');
    const clearSearchBtn = document.getElementById('clearSearch');
    const sortSelect = document.getElementById('sortSelect');
    const resultsCount = document.getElementById('resultsCount');
    const loadingState = document.getElementById('loadingState');
    const errorState = document.getElementById('errorState');
    const errorMessage = document.getElementById('errorMessage');
    const retryButton = document.getElementById('retryButton');
    const catalogGrid = document.getElementById('catalogGrid');
    const noResultsState = document.getElementById('noResultsState');

    // ============================================================
    // Unicode Normalization for Search
    // ============================================================

    /**
     * Normalize a string for search comparison.
     * Handles Unicode normalization, diacritics, and case.
     */
    function normalizeForSearch(text) {
        if (!text) return '';

        // Step 1: Unicode NFKD normalization to decompose characters
        // e.g., "ō" (U+014D) -> "o" + combining macron
        let normalized = text.toLowerCase().trim().normalize('NFKD');

        // Step 2: Remove combining marks (diacritics)
        // This converts "ō" -> "o", "é" -> "e", etc.
        normalized = normalized.replace(/[\u0300-\u036f\u0483-\u0489\u048A-\u048F\u0490-\u04FF]/g, '');

        // Step 3: Replace common Unicode variants with ASCII equivalents
        // Handle full-width characters and other special forms
        normalized = normalized
            // Common ligatures
            .replace(/æ/g, 'ae')
            .replace(/œ/g, 'oe')
            // Full-width Latin characters
            .replace(/[\uff21-\uff3a]/g, c => String.fromCharCode(c.charCodeAt(0) - 0xFF21 + 0x61))
            .replace(/[\uff41-\uff5a]/g, c => String.fromCharCode(c.charCodeAt(0) - 0xFF41 + 0x61))
            // Additional special characters
            .replace(/\s+/g, ' ')
            .replace(/[^\w\s-]/g, '');

        return normalized;
    }

    /**
     * Check if a text matches the search query.
     */
    function matchesSearch(text, query) {
        if (!query || !text) return true;

        const normalizedText = normalizeForSearch(text);
        const normalizedQuery = normalizeForSearch(query);

        // Direct substring match
        if (normalizedText.includes(normalizedQuery)) return true;

        // Word-by-word matching
        const queryWords = normalizedQuery.split(/\s+/).filter(w => w.length > 0);
        return queryWords.every(word => normalizedText.includes(word));
    }

    // ============================================================
    // Sorting
    // ============================================================

    function sortIdentities(identities, sortBy) {
        const sorted = [...identities];

        switch (sortBy) {
            case 'name-asc':
                sorted.sort((a, b) => normalizeForSearch(a.identityName).localeCompare(normalizeForSearch(b.identityName)));
                break;
            case 'name-desc':
                sorted.sort((a, b) => normalizeForSearch(b.identityName).localeCompare(normalizeForSearch(a.identityName)));
                break;
            case 'character-asc':
                sorted.sort((a, b) => normalizeForSearch(a.characterName).localeCompare(normalizeForSearch(b.characterName)));
                break;
            case 'character-desc':
                sorted.sort((a, b) => normalizeForSearch(b.characterName).localeCompare(normalizeForSearch(a.characterName)));
                break;
            case 'rarity-asc':
                sorted.sort((a, b) => a.rarity - b.rarity);
                break;
            case 'rarity-desc':
                sorted.sort((a, b) => b.rarity - a.rarity);
                break;
            default:
                break;
        }

        return sorted;
    }

    // ============================================================
    // Rendering
    // ============================================================

    /**
     * Create an HTML string for a single identity card.
     */
    function createCardHTML(identity) {
        const rarityClass = `Rar${identity.rarity}`;
        const isSelected = identity.isSelected ? 'is-selected' : '';
        const ariaPressed = identity.isSelected ? 'true' : 'false';

        // Escape HTML entities in identity name
        const escapedName = escapeHtml(identity.identityName);

        return `
            <div class="identity-card ${isSelected}" 
                 data-id="${identity.characterName}||${identity.identityName}" 
                 data-rarity="${identity.rarity}"
                 tabindex="0" 
                 role="button" 
                 aria-pressed="${ariaPressed}"
                 aria-label="${escapedName}">
                <div class="identity-card__visual">
                    <img 
                        src="${escapeHtml(identity.imageUrl)}" 
                        alt="${escapedName}"
                        loading="lazy"
                        decoding="async"
                        width="125"
                        height="193"
                        class="identity-card__image"
                        onerror="this.classList.add('img-fallback'); this.alt='Image unavailable';"
                    />
                    <div class="identity-card__rarity ${rarityClass} IDRar"></div>
                </div>
                <div class="identity-card__name">${escapedName}</div>
            </div>
        `;
    }

    /**
     * Escape HTML special characters to prevent XSS.
     */
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }

    /**
     * Render the filtered identities to the catalog grid.
     */
    function renderCatalog() {
        const query = searchInput.value.trim();

        // Filter - only search by identityName
        filteredIdentities = allIdentities.filter(identity => {
            return matchesSearch(identity.identityName, query);
        });

        // Sort
        const sortBy = sortSelect.value;
        filteredIdentities = sortIdentities(filteredIdentities, sortBy);

        // Update UI state
        if (filteredIdentities.length === 0 && allIdentities.length > 0) {
            catalogGrid.style.display = 'none';
            noResultsState.style.display = 'flex';
            resultsCount.textContent = 'No identities found';
        } else {
            catalogGrid.style.display = 'grid';
            noResultsState.style.display = 'none';
            resultsCount.textContent = filteredIdentities.length === allIdentities.length
                ? `${allIdentities.length} identities`
                : `${filteredIdentities.length} of ${allIdentities.length} identities`;
        }

        // Render cards
        const html = filteredIdentities.map(identity => createCardHTML(identity)).join('');
        catalogGrid.innerHTML = html;

        // Attach event listeners to cards
        attachCardListeners();
    }

    // ============================================================
    // Card Interaction
    // ============================================================

    /**
     * Attach click and keyboard listeners to all identity cards.
     */
    function attachCardListeners() {
        const cards = catalogGrid.querySelectorAll('.identity-card');

        cards.forEach(card => {
            // Click handler
            card.addEventListener('click', handleCardClick);

            // Keyboard handler
            card.addEventListener('keydown', handleCardKeydown);
        });
    }

    /**
     * Handle card click - toggle selection state.
     */
    function handleCardClick(e) {
        const card = e.currentTarget;
        const dataId = card.getAttribute('data-id');
        const [charName, identityName] = dataId.split('||');

        // Find the identity in allIdentities
        const identity = allIdentities.find(i =>
            i.characterName === charName && i.identityName === identityName
        );

        if (!identity) return;

        // Toggle selection
        identity.isSelected = !identity.isSelected;

        // Update DOM
        updateCardDOM(card, identity);
    }

    /**
     * Handle keyboard interaction (Enter and Space).
     */
    function handleCardKeydown(e) {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            handleCardClick(e);
        }
    }

    /**
     * Update the DOM element to reflect the identity's selection state.
     */
    function updateCardDOM(card, identity) {
        const isSelected = identity.isSelected;

        card.classList.toggle('is-selected', isSelected);
        card.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
    }

    // ============================================================
    // API Loading
    // ============================================================

    /**
     * Load identities from the API.
     */
    async function loadIdentities() {
        // Show loading state
        loadingState.style.display = 'flex';
        errorState.style.display = 'none';
        catalogGrid.style.display = 'none';
        noResultsState.style.display = 'none';

        try {
            const response = await fetch('/api/identities');

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const identities = await response.json();

            if (!Array.isArray(identities)) {
                throw new Error('Invalid response format');
            }

            // Ensure all identities have isSelected property
            allIdentities = identities.map(identity => ({
                ...identity,
                isSelected: identity.isSelected === true
            }));

            // Render initial catalog
            renderCatalog();

            // Show catalog
            loadingState.style.display = 'none';
            catalogGrid.style.display = 'grid';

        } catch (error) {
            console.error('Failed to load identities:', error);
            loadingState.style.display = 'none';
            errorState.style.display = 'flex';
            errorMessage.textContent = `Failed to load identities: ${error.message || 'Unknown error'}. Please try again later.`;
        }
    }

    // ============================================================
    // Event Handlers
    // ============================================================

    let searchTimeout = null;

    function handleSearchInput() {
        // Show/hide clear button
        clearSearchBtn.style.display = searchInput.value.trim() ? 'block' : 'none';

        // Debounce search input
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            renderCatalog();
        }, 150);
    }

    function handleClearSearch() {
        searchInput.value = '';
        clearSearchBtn.style.display = 'none';
        renderCatalog();
        searchInput.focus();
    }

    function handleSortChange() {
        renderCatalog();
    }

    function handleRetry() {
        loadIdentities();
    }

    // ============================================================
    // Event Listeners
    // ============================================================

    searchInput.addEventListener('input', handleSearchInput);
    clearSearchBtn.addEventListener('click', handleClearSearch);
    sortSelect.addEventListener('change', handleSortChange);
    retryButton.addEventListener('click', handleRetry);

    // Keyboard shortcut: Escape to clear search
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && searchInput.value.trim()) {
            handleClearSearch();
        }
    });

    // ============================================================
    // Initialize
    // ============================================================

    // Start loading when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', loadIdentities);
    } else {
        loadIdentities();
    }
})();
