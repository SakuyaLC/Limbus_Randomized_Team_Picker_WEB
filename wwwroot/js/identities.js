/**
 * Limbus Randomized Team Picker - Identity Catalog JavaScript
 * Handles loading, searching, sorting, selection, and team assembly.
 */

(function () {
    'use strict';

    // ============================================================
    // State
    // ============================================================
    let allIdentities = [];
    let filteredIdentities = [];
    let assembledTeam = null;

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
    const selectAllBtn = document.getElementById('selectAllBtn');
    const clearBtn = document.getElementById('clearBtn');
    const assembleBtn = document.getElementById('assembleBtn');
    const teamSection = document.getElementById('teamSection');
    const teamGrid = document.getElementById('teamGrid');
    const emptyTeamState = document.getElementById('emptyTeamState');

    // ============================================================
    // Unicode Normalization for Search
    // ============================================================

    function normalizeForSearch(text) {
        if (!text) return '';
        let normalized = text.toLowerCase().trim().normalize('NFKD');
        normalized = normalized.replace(/[\u0300-\u036f\u0483-\u0489\u048A-\u048F\u0490-\u04FF]/g, '');
        normalized = normalized
            .replace(/æ/g, 'ae')
            .replace(/œ/g, 'oe')
            .replace(/[\uff21-\uff3a]/g, c => String.fromCharCode(c.charCodeAt(0) - 0xFF21 + 0x61))
            .replace(/[\uff41-\uff5a]/g, c => String.fromCharCode(c.charCodeAt(0) - 0xFF41 + 0x61))
            .replace(/\s+/g, ' ')
            .replace(/[^\w\s-]/g, '');
        return normalized;
    }

    function matchesSearch(text, query) {
        if (!query || !text) return true;
        const normalizedText = normalizeForSearch(text);
        const normalizedQuery = normalizeForSearch(query);
        if (normalizedText.includes(normalizedQuery)) return true;
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
    // Rendering - Identity Cards
    // ============================================================

    function createCardHTML(identity) {
        const rarityClass = `Rar${identity.rarity}`;
        const isSelected = identity.isSelected ? 'is-selected' : '';
        const ariaPressed = identity.isSelected ? 'true' : 'false';
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

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }

    function renderCatalog() {
        const query = searchInput.value.trim();
        filteredIdentities = allIdentities.filter(identity => {
            return matchesSearch(identity.identityName, query);
        });

        const sortBy = sortSelect.value;
        filteredIdentities = sortIdentities(filteredIdentities, sortBy);

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

        const html = filteredIdentities.map(identity => createCardHTML(identity)).join('');
        catalogGrid.innerHTML = html;
        attachCardListeners();

        // Apply staggered animation delays to catalog cards (6ms for smooth performance with large lists)
        const cards = catalogGrid.querySelectorAll('.identity-card');
        cards.forEach((card, index) => {
            card.style.animationDelay = `${index * 0.006}s`;
        });
    }

    // ============================================================
    // Card Interaction
    // ============================================================

    function attachCardListeners() {
        const cards = catalogGrid.querySelectorAll('.identity-card');
        cards.forEach(card => {
            card.addEventListener('click', handleCardClick);
            card.addEventListener('keydown', handleCardKeydown);
        });
    }

    function handleCardClick(e) {
        const card = e.currentTarget;
        const dataId = card.getAttribute('data-id');
        const [charName, identityName] = dataId.split('||');
        const identity = allIdentities.find(i =>
            i.characterName === charName && i.identityName === identityName
        );
        if (!identity) return;
        identity.isSelected = !identity.isSelected;
        updateCardDOM(card, identity);
    }

    function handleCardKeydown(e) {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            handleCardClick(e);
        }
    }

    function updateCardDOM(card, identity) {
        const isSelected = identity.isSelected;
        card.classList.toggle('is-selected', isSelected);
        card.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
    }

    // ============================================================
    // Team Rendering
    // ============================================================

    function createTeamCardHTML(member) {
        const characterName = escapeHtml(member.characterName);

        if (!member.identity) {
            return `
                <div class="team-card team-card--empty" aria-label="${characterName}: No selected identity">
                    <div class="team-card__header">
                        <span class="team-card__character">${characterName}</span>
                    </div>
                    <div class="team-card__visual team-card__visual--empty">
                        <div class="team-card__empty-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                                <circle cx="12" cy="12" r="10" />
                                <line x1="12" y1="8" x2="12" y2="12" />
                                <line x1="12" y1="16" x2="12.01" y2="16" />
                            </svg>
                        </div>
                        <span class="team-card__empty-text">No selected identity</span>
                    </div>
                </div>
            `;
        }

        const identity = member.identity;
        const rarityClass = `Rar${identity.rarity}`;
        const escapedName = escapeHtml(identity.identityName);

        return `
            <div class="team-card" aria-label="${characterName}: ${escapedName}">
                <div class="team-card__header">
                    <span class="team-card__character">${characterName}</span>
                </div>
                <div class="team-card__visual">
                    <img 
                        src="${escapeHtml(identity.imageUrl)}" 
                        alt="${escapedName}"
                        width="125"
                        height="193"
                        class="team-card__image"
                    />
                    <div class="team-card__rarity ${rarityClass} IDRar"></div>
                </div>
                <div class="team-card__name">${escapedName}</div>
            </div>
        `;
    }

    function renderTeam(team) {
        if (!team || !team.members || team.members.length === 0) {
            teamSection.style.display = 'none';
            emptyTeamState.style.display = 'flex';
            return;
        }

        emptyTeamState.style.display = 'none';
        teamSection.style.display = 'block';

        const html = team.members.map(member => createTeamCardHTML(member)).join('');
        teamGrid.innerHTML = html;

        // Apply staggered animation delays to team cards (6ms for smooth appearance)
        const cards = teamGrid.querySelectorAll('.team-card');
        cards.forEach((card, index) => {
            card.style.animationDelay = `${index * 0.006}s`;
        });
    }

    // ============================================================
    // API Loading
    // ============================================================

    async function loadIdentities() {
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

            allIdentities = identities.map(identity => ({
                ...identity,
                // Map selectionKey to identityPageUrl for backward compatibility with selection logic
                identityPageUrl: identity.selectionKey || identity.identityPageUrl,
                isSelected: identity.isSelected === true
            }));

            renderCatalog();
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
    // Team Assembly
    // ============================================================

    async function assembleTeam() {
        const selectedUrls = allIdentities
            .filter(i => i.isSelected)
            .map(i => i.identityPageUrl);

        if (selectedUrls.length === 0) {
            assembleBtn.classList.add('btn-assemble--no-selection');
            setTimeout(() => {
                assembleBtn.classList.remove('btn-assemble--no-selection');
            }, 1000);
            return;
        }

        assembleBtn.disabled = true;
        assembleBtn.textContent = 'Assembling...';

        try {
            console.log('Sending request to /api/team/assemble with', selectedUrls.length, 'selected identities');

            const requestBody = JSON.stringify({ selectedIdentityPageUrls: selectedUrls });
            console.log('Request body:', requestBody);

            const response = await fetch('/api/team/assemble', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: requestBody
            });

            console.log('Response status:', response.status);
            console.log('Response headers:', Object.fromEntries(response.headers.entries()));

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Error response:', errorText);
                throw new Error(`HTTP error! status: ${response.status}. Details: ${errorText}`);
            }

            const team = await response.json();
            console.log('Team assembled successfully:', team);
            assembledTeam = team;
            renderTeam(team);

        } catch (error) {
            console.error('Failed to assemble team:', error);
            alert(`Failed to assemble team: ${error.message || 'Unknown error'}\n\nCheck console (F12) for details.`);
        } finally {
            assembleBtn.disabled = false;
            updateAssembleBtnText();
        }
    }

    function updateAssembleBtnText() {
        assembleBtn.textContent = 'Assemble Team';
    }

    // ============================================================
    // Event Handlers
    // ============================================================

    let searchTimeout = null;

    function handleSearchInput() {
        clearSearchBtn.style.display = searchInput.value.trim() ? 'block' : 'none';
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

    function handleSelectAll() {
        allIdentities.forEach(identity => {
            identity.isSelected = true;
        });
        renderCatalog();
    }

    function handleClear() {
        allIdentities.forEach(identity => {
            identity.isSelected = false;
        });
        assembledTeam = null;
        renderTeam(null);
        renderCatalog();
    }

    function handleAssemble() {
        assembleTeam();
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
    selectAllBtn.addEventListener('click', handleSelectAll);
    clearBtn.addEventListener('click', handleClear);
    assembleBtn.addEventListener('click', handleAssemble);
    retryButton.addEventListener('click', handleRetry);

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && searchInput.value.trim()) {
            handleClearSearch();
        }
    });

    // ============================================================
    // Initialize
    // ============================================================

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', loadIdentities);
    } else {
        loadIdentities();
    }
})();
