<script lang="ts">
  export let title: string;
</script>

<div class="col-auto">
  <div class="card" style="width: 18rem;">
    <div class="card-body">
      {#if title}
        <h5 class="card-title">{title}</h5>
      {/if}
      {#if $$slots.text}
        <p class="card-text"><slot name="text" /></p>
      {/if}
      {#if $$slots.extra}
        <slot name="extra" />
      {/if}
    </div>
  </div>
</div>
