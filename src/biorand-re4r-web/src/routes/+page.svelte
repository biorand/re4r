<script>
  import Card from '$lib/card.svelte';
</script>

<div class="container my-3">
  <div class="card">
    <div class="card-body">
      <div class="row">
        <h3>Your randomizer is ready!</h3>
        <p>Download the appropriate file and enjoy!</p>
      </div>
      <div class="row">
        <Card title="Patch">
          <span slot="text">Simply drop this file into your RE 4 install folder.</span>
          <span slot="extra"
            ><a id="btn-download-pak" href="#" class="btn btn-success">Download</a></span
          >
        </Card>
        <Card title="Fluffy Mod">
          <span slot="text">Add this to Fluffy Mod manager and enable it.</span>
          <span slot="extra"
            ><a id="btn-download-mod" href="#" class="btn btn-success">Download</a></span
          >
        </Card>
      </div>
    </div>
  </div>
</div>
