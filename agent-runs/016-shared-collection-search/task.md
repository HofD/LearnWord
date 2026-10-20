# Shared collection search

Add the missing search control to the existing shared collection pager. Search the entire readable source collection on the server, matching word value OR translation, not only the downloaded page. Preserve safe readonly DTO and existing owner/share/fork authorization. Keep existing API calls compatible with an optional search parameter.

Acceptance: compact RU/EN search in reused pager; submit via Enter/Search consistent with server-side Explore; no request on each keystroke; trim/case-insensitive matching; matches filtered before count/order/skip/take; totalCount counts matching cards and cardsCount stays collection total; return all active words of matched cards; reset page1 on search/size/route, keep applied search on paging/size/retry, clear restores collection, meaningful empty/search count states; multiword matches counted once; hidden/deleted/foreign/private content stays unavailable; tests and Docker+browser verification.
