# Shared automatic search

Correct prior manual-submit UX. Shared card search must react automatically to input, without Find/Enter requirement or search button, matching owner input/pager appearance. Preserve whole-collection server filtering/pagination rather than downloading every card. Apply ~300ms debounce; input remains editable while pending, cancel superseded reads and ignore stale results. Clear restores immediately, search resets page1; paging/size/retry use latest applied query; route/destroy cancel timers/reads. Existing owner and Explore behavior unchanged.

Acceptance: no Find button/form submit; rapid typing coalesces; editable pending input; latest query only; native clear and Reset Search restore full source; query/page/size consistency; no stale result/error after query/route changes; focus/mobile/readonly permissions intact. Use existing Angular/RxJS tests, Docker build and browser verification. Backend/API unchanged.
